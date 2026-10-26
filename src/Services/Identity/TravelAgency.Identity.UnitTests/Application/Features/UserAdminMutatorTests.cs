using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Admin;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class UserAdminMutatorTests
{
    private readonly Mock<ICurrentUserService> _current = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUserAdminAuditRepository> _audit = new();
    private readonly Mock<IRefreshTokenRepository> _refresh = new();
    private readonly Mock<ISessionRevocationStore> _sessions = new();
    private readonly UserAdminMutator _mutator;
    private readonly Guid _actorId = Guid.NewGuid();

    public UserAdminMutatorTests()
    {
        _current.Setup(c => c.IsAuthenticated).Returns(true);
        _current.Setup(c => c.UserId).Returns(_actorId);
        _users.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _refresh.Setup(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessions.Setup(s => s.SetAsync(
                It.IsAny<Guid>(),
                It.IsAny<long>(),
                It.IsAny<bool>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessions.Setup(s => s.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mutator = new UserAdminMutator(
            _current.Object,
            _users.Object,
            _audit.Object,
            _refresh.Object,
            new ImmediateLock(),
            Mock.Of<TravelAgency.Identity.Application.Abstractions.IUnitOfWork>(),
            _sessions.Object,
            Options.Create(new JwtSettings { AccessTokenExpirationMinutes = 15 }),
            NullLogger<UserAdminMutator>.Instance);
    }

    [Fact]
    public async Task ChangeRole_Upgrade_DoesNotRevokeSessions()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", "+79990000000");
        SetupUsers(target);

        var result = await _mutator.ChangeRoleAsync(target.Id, "Manager", CancellationToken.None);

        result.Message.Should().Be(UserAdminText.RoleChanged);
        result.User.Role.Should().Be("Manager");
        result.User.Email.Should().Be("client@example.com");
        target.SessionVersion.Should().Be(0);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _sessions.Verify(
            s => s.SetAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _audit.Verify(a => a.Stage(It.Is<UserAdminAudit>(entry => entry.Action == UserAdminAudit.ActionRoleChanged)), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_Downgrade_RevokesRefreshAndPublishesVersion()
    {
        var target = User.Create("manager@example.com", "hash", "Мария", "Иванова", null, UserRole.Manager);
        SetupUsers(target);

        var result = await _mutator.ChangeRoleAsync(target.Id, "Client", CancellationToken.None);

        result.User.Role.Should().Be("Client");
        target.SessionVersion.Should().Be(1);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
        _sessions.Verify(
            s => s.SetAsync(target.Id, 1, false, TimeSpan.FromMinutes(17), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessions.Verify(s => s.PublishAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeRole_SameRole_DoesNotWriteAudit()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        SetupUsers(target);

        var result = await _mutator.ChangeRoleAsync(target.Id, "Client", CancellationToken.None);

        result.Message.Should().Be(UserAdminText.RoleUnchanged);
        _audit.Verify(a => a.Stage(It.IsAny<UserAdminAudit>()), Times.Never);
    }

    [Fact]
    public async Task ChangeRole_SelfDowngrade_Throws()
    {
        var self = User.Create("admin@example.com", "hash", "Админ", "Главный", null, UserRole.Admin, _actorId);
        SetupUsers(self);

        var act = async () => await _mutator.ChangeRoleAsync(self.Id, "Manager", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage(UserAdminText.SelfDowngrade);
        _audit.Verify(a => a.Stage(It.IsAny<UserAdminAudit>()), Times.Never);
    }

    [Fact]
    public async Task ChangeRole_LastActiveAdmin_Throws()
    {
        var target = User.Create("admin@example.com", "hash", "Админ", "Последний", null, UserRole.Admin);
        SetupUsers(target);
        _users.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = async () => await _mutator.ChangeRoleAsync(target.Id, "Manager", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage(UserAdminText.LastAdmin);
    }

    [Fact]
    public async Task Block_RevokesSessionsAndStoresReason()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        SetupUsers(target);

        var result = await _mutator.BlockAsync(target.Id, "  спам  ", CancellationToken.None);

        result.Message.Should().Be(UserAdminText.UserBlocked);
        result.User.Status.Should().Be("Blocked");
        result.User.BlockReason.Should().Be("спам");
        target.SessionVersion.Should().Be(1);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
        _sessions.Verify(s => s.SetAsync(target.Id, 1, true, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
        _audit.Verify(a => a.Stage(It.Is<UserAdminAudit>(entry =>
            entry.Action == UserAdminAudit.ActionBlocked && entry.Reason == "спам")), Times.Once);
    }

    [Fact]
    public async Task Block_RetriesTransientSessionPublishThenSucceeds()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        SetupUsers(target);
        _sessions.SetupSequence(s => s.SetAsync(
                target.Id,
                1,
                true,
                TimeSpan.FromMinutes(17),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("redis"))
            .Returns(Task.CompletedTask)
            .Returns(Task.CompletedTask);
        _sessions.SetupSequence(s => s.PublishAsync(target.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("redis"))
            .Returns(Task.CompletedTask);

        var result = await _mutator.BlockAsync(target.Id, "спам", CancellationToken.None);

        result.Message.Should().Be(UserAdminText.UserBlocked);
        target.SessionVersion.Should().Be(1);
        _sessions.Verify(
            s => s.SetAsync(target.Id, 1, true, TimeSpan.FromMinutes(17), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _sessions.Verify(s => s.PublishAsync(target.Id, It.IsAny<CancellationToken>()), Times.Exactly(2));
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Block_KeepsMutationWhenSessionPublishRetriesAreExhausted()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        SetupUsers(target);
        var logger = new CollectingLogger();
        var mutator = new UserAdminMutator(
            _current.Object,
            _users.Object,
            _audit.Object,
            _refresh.Object,
            new ImmediateLock(),
            Mock.Of<TravelAgency.Identity.Application.Abstractions.IUnitOfWork>(),
            _sessions.Object,
            Options.Create(new JwtSettings { AccessTokenExpirationMinutes = 15 }),
            logger);
        _sessions.Setup(s => s.SetAsync(
                target.Id,
                1,
                true,
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessions.Setup(s => s.PublishAsync(target.Id, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("redis"));

        var result = await mutator.BlockAsync(target.Id, "спам", CancellationToken.None);

        result.Message.Should().Be(UserAdminText.UserBlocked);
        result.User.Status.Should().Be("Blocked");
        target.SessionVersion.Should().Be(1);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(target.Id, It.IsAny<CancellationToken>()), Times.Once);
        _sessions.Verify(
            s => s.SetAsync(target.Id, 1, true, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        _sessions.Verify(s => s.PublishAsync(target.Id, It.IsAny<CancellationToken>()), Times.Exactly(3));
        logger.Levels.Should().Equal(LogLevel.Error);
    }

    [Fact]
    public async Task Block_Self_Throws()
    {
        var self = User.Create("admin@example.com", "hash", "Админ", "Главный", null, UserRole.Admin, _actorId);
        SetupUsers(self);

        var act = async () => await _mutator.BlockAsync(self.Id, "причина", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage(UserAdminText.SelfBlock);
    }

    [Fact]
    public async Task Block_LastActiveAdmin_Throws()
    {
        var target = User.Create("admin@example.com", "hash", "Админ", "Последний", null, UserRole.Admin);
        SetupUsers(target);
        _users.Setup(r => r.CountActiveAdminsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var act = async () => await _mutator.BlockAsync(target.Id, "причина", CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>().WithMessage(UserAdminText.LastAdmin);
    }

    [Fact]
    public async Task Block_WhenAlreadyBlocked_DoesNotDuplicateAudit()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        target.Block("спам", _actorId, DateTime.UtcNow);
        SetupUsers(target);

        var act = async () => await _mutator.BlockAsync(target.Id, "ещё раз", CancellationToken.None);

        await act.Should().ThrowAsync<ConflictException>().WithMessage(UserAdminText.AlreadyBlocked);
        _audit.Verify(a => a.Stage(It.IsAny<UserAdminAudit>()), Times.Never);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Unblock_PublishesOpenSessionAndDoesNotRestoreRefresh()
    {
        var target = User.Create("client@example.com", "hash", "Иван", "Петров", null);
        target.Block("спам", _actorId, DateTime.UtcNow);
        SetupUsers(target);

        var result = await _mutator.UnblockAsync(target.Id, CancellationToken.None);

        result.Message.Should().Be(UserAdminText.UserUnblocked);
        result.User.Status.Should().Be("Active");
        target.SessionVersion.Should().Be(1);
        _refresh.Verify(r => r.RevokeAllByUserIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _sessions.Verify(s => s.SetAsync(target.Id, 1, false, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void LikePattern_EscapesWildcards()
    {
        UserSearch.LikePattern("100%_a").Should().Be("%100\\%\\_a%");
    }

    private void SetupUsers(User target)
    {
        var actor = target.Id == _actorId
            ? target
            : User.Create("actor@example.com", "hash", "Админ", "Второй", null, UserRole.Admin, _actorId);

        _users.Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == target.Id ? target : actor);
    }

    private sealed class ImmediateLock : IUserAdminLock
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken) =>
            action(cancellationToken);
    }

    private sealed class CollectingLogger : ILogger<UserAdminMutator>
    {
        public List<LogLevel> Levels { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Levels.Add(logLevel);
            formatter(state, exception);
        }
    }
}
