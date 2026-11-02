using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using TravelAgency.Identity.Application.Abstractions;
using TravelAgency.Identity.Application.DTOs;
using TravelAgency.Identity.Application.Exceptions;
using TravelAgency.Identity.Application.Features.Auth;
using TravelAgency.Identity.Application.Features.Auth.Account;
using TravelAgency.Identity.Application.Features.Auth.Commands.Register;
using TravelAgency.Identity.Application.Interfaces;
using TravelAgency.Identity.Application.Settings;
using TravelAgency.Identity.Domain.Entities;
using TravelAgency.Identity.Domain.Enums;
using TravelAgency.Identity.Domain.Interfaces;
using TravelAgency.Identity.Infrastructure.Services;
using TravelAgency.Shared.Contracts.Abstractions;
using TravelAgency.Shared.Contracts.Auth;
using TravelAgency.Shared.Contracts.Authorization;

namespace TravelAgency.Identity.UnitTests.Application.Features;

public class AccountLinkHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 2, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IAccountLinkStore> _store = new();
    private readonly RecordingMail _mail = new();
    private readonly FakeQuota _quota = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ILockoutService> _lockout = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<ICurrentUserService> _current = new();
    private readonly Mock<IRefreshTokenRepository> _refresh = new();
    private readonly Mock<IJwtTokenService> _jwt = new();
    private readonly Mock<ISessionRevocationStore> _sessions = new();
    private readonly PasswordHasherService _realHasher = new();
    private readonly AccountLinkSettings _links = new()
    {
        PublicBaseUrl = "https://travelagency.example",
        PasswordResetLifetime = TimeSpan.FromHours(1),
        EmailChangeLifetime = TimeSpan.FromHours(24)
    };
    private readonly List<string> _trace = [];

    public AccountLinkHandlerTests()
    {
        _uow.Setup(unit => unit.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Callback(() => _trace.Add("save"))
            .ReturnsAsync(0);
        _mail.OnCall = () => _trace.Add("port");
        _store.Setup(store => store.ReplacePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _store.Setup(store => store.ReplaceEmailChangeAsync(It.IsAny<Guid>(), It.IsAny<EmailChangeRequest?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _sessions.Setup(store => store.SetAsync(It.IsAny<Guid>(), It.IsAny<long>(), It.IsAny<bool>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _sessions.Setup(store => store.PublishAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    [Theory]
    [InlineData("missing@example.com", false, false)]
    [InlineData("known@example.com", true, false)]
    [InlineData("blocked@example.com", true, true)]
    public async Task Tk8_ResetRequest_SameAnswer_AndLetterOnlyWhenAllowed(string email, bool known, bool blocked)
    {
        User? user = known ? UserOf(email, blocked) : null;
        _users.Setup(repo => repo.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .Callback(() => _trace.Add("lookup"))
            .ReturnsAsync(user);

        var first = await ResetHandler().Handle(new RequestPasswordResetCommand($"  {email.ToUpperInvariant()}  "), CancellationToken.None);

        first.Message.Should().Be(AccountLinkMessages.PasswordResetRequested);
        _trace.Should().Equal("lookup", "port", "save");
        _mail.Calls.Should().ContainSingle();
        if (known && !blocked)
        {
            _mail.Calls[0].Deliver.Should().BeTrue();
            _mail.Io.Should().Be(1);
            _store.Verify(store => store.ReplacePasswordResetAsync(user!.Id, It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()));
        }
        else
        {
            _mail.Calls[0].Deliver.Should().BeFalse();
            _mail.Io.Should().Be(0);
            _quota.Keys.Should().BeEmpty();
            _store.Verify(store => store.ReplacePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        _lockout.Verify(service => service.ResetFailedAttempts(It.IsAny<string>()), Times.Never);
        _lockout.Verify(service => service.RecordFailedAttempt(It.IsAny<string>()), Times.Never);
        first.Message.Should().NotContain("поддерж");
    }

    [Fact]
    public async Task Tk8_Limit_PortReject_AndStoreFailure_KeepThePreviousLink()
    {
        var user = UserOf("known@example.com", blocked: false);
        _users.Setup(repo => repo.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>()))
            .Callback(() => _trace.Add("lookup"))
            .ReturnsAsync(user);
        _quota.Count = 3;
        var limited = await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        limited.Message.Should().Be(AccountLinkMessages.PasswordResetRequested);
        _mail.Calls.Should().ContainSingle(call => !call.Deliver);
        _store.Verify(store => store.ReplacePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);

        _mail.Reset();
        _quota.Count = 0;
        _quota.Fail = true;
        await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        _mail.Calls.Should().ContainSingle(call => !call.Deliver);

        _mail.Reset();
        _quota.Fail = false;
        _mail.Accept = false;
        await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        _store.Verify(store => store.ReplacePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Never);
        _quota.Count.Should().Be(0);
    }

    [Fact]
    public async Task Tk9_Inspect_DoesNotConsume_AndLifetimeIsOneHour()
    {
        var user = UserOf("known@example.com", blocked: false);
        PasswordResetToken? created = null;
        _users.Setup(repo => repo.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _store.Setup(store => store.ReplacePasswordResetAsync(user.Id, It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, PasswordResetToken, CancellationToken>((_, token, _) => created = token)
            .ReturnsAsync(true);
        await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        created.Should().NotBeNull();
        created!.ExpiresAt.Should().Be(Now.UtcDateTime.AddHours(1));

        var secret = SecretFrom(_mail.Calls.Single(call => call.Deliver).LinkUrl!);
        created.TokenHash.Should().Be(LinkSecret.Hash(secret));
        created.TokenHash.Should().HaveLength(64);
        _store.Setup(store => store.FindPasswordResetAsync(created.TokenHash, It.IsAny<CancellationToken>())).ReturnsAsync(created);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var ready = await new InspectPasswordResetCommandHandler(_store.Object, _users.Object, new FixedClock(Now))
            .Handle(new InspectPasswordResetCommand(secret), CancellationToken.None);
        var again = await new InspectPasswordResetCommandHandler(_store.Object, _users.Object, new FixedClock(Now))
            .Handle(new InspectPasswordResetCommand(secret), CancellationToken.None);
        ready.Outcome.Should().Be("ready");
        again.Outcome.Should().Be("ready");
        _store.Verify(store => store.TryConsumePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        user.Block("причина", Guid.NewGuid(), Now.UtcDateTime);
        var blocked = await new InspectPasswordResetCommandHandler(_store.Object, _users.Object, new FixedClock(Now))
            .Handle(new InspectPasswordResetCommand(secret), CancellationToken.None);
        blocked.Outcome.Should().Be("invalid");
        user.Unblock(Now.UtcDateTime);
        var restored = await new InspectPasswordResetCommandHandler(_store.Object, _users.Object, new FixedClock(Now))
            .Handle(new InspectPasswordResetCommand(secret), CancellationToken.None);
        restored.Outcome.Should().Be("ready");
    }

    [Theory]
    [InlineData("current")]
    [InlineData("other")]
    [InlineData("none")]
    public async Task Tk10_CompleteReset_RevokesSessions_AndSendsNotice(string session)
    {
        var user = UserOf("known@example.com", blocked: false);
        var oldHash = user.PasswordHash;
        var row = PasswordResetToken.Create(user.Id, "secret", Now.UtcDateTime.AddHours(1));
        _store.Setup(store => store.FindPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns<string>(password => _realHasher.Hash(password));
        _store.Setup(store => store.TryConsumePasswordResetAsync(row.Id, user, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, User, string, CancellationToken>((_, account, hash, _) =>
            {
                account.ChangePassword(hash);
                account.RevokeSessions();
            })
            .ReturnsAsync(true);
        SignIn(session == "none" ? null : session == "current" ? user.Id : Guid.NewGuid());

        var result = await CompleteHandler().Handle(
            new CompletePasswordResetCommand("secret", "Password1", "Password1"),
            CancellationToken.None);

        result.Outcome.Should().Be("changed");
        result.Session.Should().Be(session);
        _realHasher.Verify("Password1", user.PasswordHash).Should().BeTrue();
        _realHasher.Verify("old-password", user.PasswordHash).Should().BeFalse();
        user.PasswordHash.Should().NotBe(oldHash);
        user.SessionVersion.Should().Be(1);
        _lockout.Verify(service => service.ResetFailedAttempts(user.Email));
        _sessions.Verify(store => store.SetAsync(user.Id, 1, false, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()));
        _mail.Calls.Should().Contain(call => call.Subject == "Пароль изменён" && call.Deliver);
    }

    [Fact]
    public async Task Tk11_PasswordRule_MatchesRegistration_AndInvalidLinkStays()
    {
        var validator = new RegisterCommandValidator();
        foreach (var password in new[] { "", "short1A", new string('A', 129) + "a1", "nouppercase1", "NOLOWERCASE1", "NoDigitsHere" })
        {
            var registration = validator.Validate(new RegisterCommand(new RegisterRequest("a@b.co", password, "Анна", "Смирнова", null)));
            registration.IsValid.Should().BeFalse();
            registration.Errors.Should().OnlyContain(error => error.ErrorCode == PasswordRules.Code);
        }

        var user = UserOf("known@example.com", blocked: false);
        var row = PasswordResetToken.Create(user.Id, "secret", Now.UtcDateTime.AddHours(1));
        _store.Setup(store => store.FindPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var handler = CompleteHandler();
        var mismatch = async () => await handler.Handle(new CompletePasswordResetCommand("secret", "Password1", "Password2"), CancellationToken.None);
        await mismatch.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == PasswordRules.MismatchCode);
        var rule = async () => await handler.Handle(new CompletePasswordResetCommand("secret", "short", "short"), CancellationToken.None);
        await rule.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == PasswordRules.Code);
        _store.Verify(store => store.TryConsumePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<User>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        _store.Setup(store => store.FindPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((PasswordResetToken?)null);
        var invalid = await handler.Handle(new CompletePasswordResetCommand("missing", "Password1", "Password1"), CancellationToken.None);
        invalid.Outcome.Should().Be("invalid");

        _hasher.Setup(hasher => hasher.Hash("Password1")).Returns(user.PasswordHash);
        _store.Setup(store => store.FindPasswordResetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _store.Setup(store => store.TryConsumePasswordResetAsync(row.Id, user, user.PasswordHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var same = await handler.Handle(new CompletePasswordResetCommand("secret", "Password1", "Password1"), CancellationToken.None);
        same.Outcome.Should().Be("changed");
    }

    [Fact]
    public async Task Tk12_SettingsPassword_KeepsFailureLocal_AndIssuesANewPair()
    {
        var user = UserOf("known@example.com", blocked: false);
        SignIn(user.Id);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), user.PasswordHash)).Returns(false);
        var handler = SettingsHandler();
        var wrong = async () => await handler.Handle(new ChangePasswordCommand("nope", "Password1", "Password1"), CancellationToken.None);
        await wrong.Should().ThrowAsync<CodedBadRequestException>()
            .Where(error => error.Code == ChangePasswordCommandHandler.CurrentPasswordInvalid);
        _store.Verify(store => store.ApplySettingsPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<Func<User, RefreshToken>>(), It.IsAny<CancellationToken>()), Times.Never);
        _lockout.Verify(service => service.ResetFailedAttempts(It.IsAny<string>()), Times.Never);

        _hasher.Setup(hasher => hasher.Verify("OldPassword1", user.PasswordHash)).Returns(true);
        _hasher.Setup(hasher => hasher.Hash(It.IsAny<string>())).Returns("hashed");
        var rule = async () => await handler.Handle(new ChangePasswordCommand("OldPassword1", "short", "short"), CancellationToken.None);
        await rule.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == PasswordRules.Code);
        var mismatch = async () => await handler.Handle(new ChangePasswordCommand("OldPassword1", "Password1", "Password2"), CancellationToken.None);
        await mismatch.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == PasswordRules.MismatchCode);

        _jwt.Setup(service => service.GenerateAccessToken(It.IsAny<User>()))
            .Returns(new AuthTokensDto("access", "", Now.UtcDateTime.AddMinutes(15), default));
        _jwt.Setup(service => service.GenerateRefreshToken()).Returns("refresh-plain");
        _store.Setup(store => store.ApplySettingsPasswordAsync(user, "hashed", It.IsAny<Func<User, RefreshToken>>(), It.IsAny<CancellationToken>()))
            .Callback<User, string, Func<User, RefreshToken>, CancellationToken>((account, hash, stage, _) =>
            {
                account.ChangePassword(hash);
                account.RevokeSessions();
                stage(account);
            })
            .Returns(Task.CompletedTask);

        var tokens = await handler.Handle(new ChangePasswordCommand("OldPassword1", "Password1", "Password1"), CancellationToken.None);
        tokens.AccessToken.Should().Be("access");
        tokens.RefreshToken.Should().Be("refresh-plain");
        tokens.ExpiresAt.Should().NotBe(default);
        tokens.RefreshExpiresAt.Should().NotBe(default);
        _mail.Calls.Should().BeEmpty();
        _sessions.Verify(store => store.SetAsync(user.Id, user.SessionVersion, false, It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()));

        SignIn(null);
        var dead = async () => await handler.Handle(new ChangePasswordCommand("OldPassword1", "Password1", "Password1"), CancellationToken.None);
        await dead.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Tk13_EmailChange_NeutralAnswers_AndPairRules()
    {
        var user = UserOf("known@example.com", blocked: true);
        SignIn(user.Id);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _hasher.Setup(hasher => hasher.Verify(It.IsAny<string>(), It.IsAny<string>())).Throws(new InvalidOperationException("password"));
        var blocked = await EmailHandler().Handle(new RequestEmailChangeCommand("other@example.com", "wrong"), CancellationToken.None);
        blocked.Message.Should().Be(AccountLinkMessages.EmailChangeRequested);
        _mail.Calls.Should().BeEmpty();

        user.Unblock(Now.UtcDateTime);
        var invalid = async () => await EmailHandler().Handle(new RequestEmailChangeCommand("not-an-email", "Password1"), CancellationToken.None);
        await invalid.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == EmailAddressRules.InvalidCode);
        var same = async () => await EmailHandler().Handle(new RequestEmailChangeCommand("  KNOWN@example.com ", "wrong"), CancellationToken.None);
        await same.Should().ThrowAsync<CodedBadRequestException>().Where(error => error.Code == EmailAddressRules.UnchangedCode);

        _hasher.Setup(hasher => hasher.Verify("wrong", user.PasswordHash)).Returns(false);
        _users.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("occupancy"));
        var badPassword = async () => await EmailHandler().Handle(new RequestEmailChangeCommand("other@example.com", "wrong"), CancellationToken.None);
        await badPassword.Should().ThrowAsync<CodedBadRequestException>()
            .Where(error => error.Code == ChangePasswordCommandHandler.CurrentPasswordInvalid);

        _hasher.Setup(hasher => hasher.Verify("Password1", user.PasswordHash)).Returns(true);
        _users.Setup(repo => repo.GetByEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        _users.Setup(repo => repo.GetByEmailAsync("other@example.com", It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);
        EmailChangeRequest? created = null;
        _store.Setup(store => store.ReplaceEmailChangeAsync(user.Id, It.IsAny<EmailChangeRequest?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, EmailChangeRequest?, CancellationToken>((_, request, _) => created = request)
            .ReturnsAsync(true);
        var sent = await EmailHandler().Handle(new RequestEmailChangeCommand("other@example.com", "Password1"), CancellationToken.None);
        sent.Message.Should().Be(AccountLinkMessages.EmailChangeRequested);
        created.Should().NotBeNull();
        created!.ExpiresAt.Should().Be(Now.UtcDateTime.AddHours(24));
        created.NewEmail.Should().Be("other@example.com");
        _mail.Calls.Should().Contain(call => call.Subject == "Подтвердите новый адрес" && call.Recipient == "other@example.com");
        _mail.Calls.Should().Contain(call => call.Subject == "Запрошена смена адреса" && call.Recipient == user.Email);
        _quota.Count.Should().Be(1);

        _mail.Reset();
        _mail.FailAfter = 1;
        created = null;
        await EmailHandler().Handle(new RequestEmailChangeCommand("free@example.com", "Password1"), CancellationToken.None);
        created.Should().BeNull();

        _mail.Reset();
        _mail.FailAfter = 0;
        var owner = UserOf("taken@example.com", blocked: false);
        _users.Setup(repo => repo.GetByEmailAsync("taken@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(owner);
        await EmailHandler().Handle(new RequestEmailChangeCommand("taken@example.com", "Password1"), CancellationToken.None);
        _mail.Calls.Should().ContainSingle(call => call.Subject == "Попытка привязать ваш адрес" && call.LinkUrl == null);
        _store.Verify(store => store.ReplaceEmailChangeAsync(user.Id, null, It.IsAny<CancellationToken>()));

        _mail.Reset();
        _quota.Count = 3;
        await EmailHandler().Handle(new RequestEmailChangeCommand("other@example.com", "Password1"), CancellationToken.None);
        _mail.Calls.Should().BeEmpty();
    }

    [Fact]
    public async Task Tk14_And_Tk15_ConfirmAndCancel()
    {
        var user = UserOf("known@example.com", blocked: false);
        var other = UserOf("other@example.com", blocked: false);
        var row = EmailChangeRequest.Create(user.Id, "new@example.com", "confirm", "cancel", Now.UtcDateTime.AddHours(24));
        _store.Setup(store => store.FindEmailChangeByConfirmAsync(LinkSecret.Hash("confirm"), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _store.Setup(store => store.FindEmailChangeByCancelAsync(LinkSecret.Hash("cancel"), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _users.Setup(repo => repo.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _refresh.Setup(repo => repo.GetByTokenAsync("own-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshToken.Create(user.Id, "own-refresh", Now.UtcDateTime.AddDays(1)));
        _refresh.Setup(repo => repo.GetByTokenAsync("foreign-refresh", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshToken.Create(other.Id, "foreign-refresh", Now.UtcDateTime.AddDays(1)));
        Guid? kept = Guid.Empty;
        _store.Setup(store => store.TryConsumeEmailConfirmationAsync(row.Id, user, row.NewEmail, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, User, string, Guid?, CancellationToken>((_, account, email, keep, _) =>
            {
                kept = keep;
                account.ChangeEmail(email);
            })
            .ReturnsAsync(EmailChangeConsumption.Changed);
        SignIn(user.Id);
        var current = await ConfirmHandler().Handle(new ConfirmEmailChangeCommand("confirm", "own-refresh"), CancellationToken.None);
        current.Outcome.Should().Be("changed");
        current.Session.Should().Be("current");
        kept.Should().NotBeNull();
        user.Email.Should().Be("new@example.com");
        user.SessionVersion.Should().Be(0);

        SignIn(null);
        var none = await ConfirmHandler().Handle(new ConfirmEmailChangeCommand("confirm", null), CancellationToken.None);
        none.Session.Should().Be("none");

        SignIn(other.Id);
        var foreign = await ConfirmHandler().Handle(new ConfirmEmailChangeCommand("confirm", "foreign-refresh"), CancellationToken.None);
        foreign.Session.Should().Be("other");

        _store.Setup(store => store.TryConsumeEmailConfirmationAsync(row.Id, user, row.NewEmail, It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EmailChangeConsumption.Taken);
        var taken = await ConfirmHandler().Handle(new ConfirmEmailChangeCommand("confirm", null), CancellationToken.None);
        taken.Outcome.Should().Be("taken");

        _store.Setup(store => store.TryConsumeEmailCancellationAsync(row.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var cancelled = await new CancelEmailChangeCommandHandler(_store.Object, _users.Object, new FixedClock(Now), NullLogger<CancelEmailChangeCommandHandler>.Instance)
            .Handle(new CancelEmailChangeCommand("cancel"), CancellationToken.None);
        cancelled.Outcome.Should().Be("cancelled");

        user.Block("причина", Guid.NewGuid(), Now.UtcDateTime);
        var blocked = await new CancelEmailChangeCommandHandler(_store.Object, _users.Object, new FixedClock(Now), NullLogger<CancelEmailChangeCommandHandler>.Instance)
            .Handle(new CancelEmailChangeCommand("cancel"), CancellationToken.None);
        blocked.Outcome.Should().Be("invalid");
        _store.Verify(store => store.TryConsumeEmailCancellationAsync(row.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Tk16_MailText_Hash_AndEmptyBase()
    {
        var user = UserOf("known@example.com", blocked: false);
        _users.Setup(repo => repo.GetByEmailAsync(user.Email, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        var reset = _mail.Calls.Single();
        reset.Subject.Should().Be("Восстановление пароля");
        reset.Text.Should().Be("Чтобы задать новый пароль, перейдите по ссылке. Она действует 1 час и только один раз. Если вы не запрашивали восстановление, ничего делать не нужно.");
        reset.LinkLabel.Should().Be("Задать новый пароль");
        reset.LinkUrl.Should().StartWith("https://travelagency.example/account/password/new?token=");
        Convert.FromBase64String(Pad(SecretFrom(reset.LinkUrl!))).Should().HaveCount(32);

        _links.PublicBaseUrl = "";
        _mail.Reset();
        await ResetHandler().Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);
        _mail.Calls.Should().ContainSingle(call => !call.Deliver);
        _store.Verify(store => store.ReplacePasswordResetAsync(It.IsAny<Guid>(), It.IsAny<PasswordResetToken>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private RequestPasswordResetCommandHandler ResetHandler() => new(
        _users.Object,
        _store.Object,
        _mail,
        _quota,
        _uow.Object,
        new AccountMailComposer(Options.Create(_links)),
        Options.Create(_links),
        new FixedClock(Now),
        NullLogger<RequestPasswordResetCommandHandler>.Instance);

    private CompletePasswordResetCommandHandler CompleteHandler() => new(
        _store.Object,
        _users.Object,
        _hasher.Object,
        _lockout.Object,
        _mail,
        new AccountMailComposer(Options.Create(_links)),
        new AccountSessionPublisher(_sessions.Object, Options.Create(new JwtSettings { AccessTokenExpirationMinutes = 15 }), NullLogger<AccountSessionPublisher>.Instance),
        _current.Object,
        new FixedClock(Now),
        NullLogger<CompletePasswordResetCommandHandler>.Instance);

    private ChangePasswordCommandHandler SettingsHandler() => new(
        _current.Object,
        _users.Object,
        _hasher.Object,
        _jwt.Object,
        _store.Object,
        new AccountSessionPublisher(_sessions.Object, Options.Create(new JwtSettings { AccessTokenExpirationMinutes = 15 }), NullLogger<AccountSessionPublisher>.Instance),
        Options.Create(new JwtSettings { RefreshTokenExpirationDays = 7 }));

    private RequestEmailChangeCommandHandler EmailHandler() => new(
        _current.Object,
        _users.Object,
        _hasher.Object,
        _store.Object,
        _mail,
        _quota,
        new AccountMailComposer(Options.Create(_links)),
        Options.Create(_links),
        new FixedClock(Now),
        NullLogger<RequestEmailChangeCommandHandler>.Instance);

    private ConfirmEmailChangeCommandHandler ConfirmHandler() => new(
        _store.Object,
        _users.Object,
        _refresh.Object,
        _current.Object,
        new FixedClock(Now),
        NullLogger<ConfirmEmailChangeCommandHandler>.Instance);

    private void SignIn(Guid? userId)
    {
        _current.SetupGet(user => user.IsAuthenticated).Returns(userId is not null);
        _current.SetupGet(user => user.UserId).Returns(userId ?? Guid.Empty);
    }

    private static User UserOf(string email, bool blocked)
    {
        var user = User.Create(email, new PasswordHasherService().Hash("old-password"), "Анна", "Смирнова", null, UserRole.Client);
        if (blocked)
            user.Block("причина", Guid.NewGuid(), Now.UtcDateTime);
        return user;
    }

    private static string SecretFrom(string link) => link.Split("token=", StringSplitOptions.None)[1];

    private static string Pad(string secret)
    {
        var padded = secret.Replace('-', '+').Replace('_', '/');
        return padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingMail : IAccountMailSender
    {
        public List<AccountMail> Calls { get; } = [];
        public bool Accept { get; set; } = true;
        public int FailAfter { get; set; }
        public int Io { get; private set; }
        public Action? OnCall { get; set; }

        public Task<bool> SendAsync(AccountMail mail, CancellationToken cancellationToken = default)
        {
            Calls.Add(mail);
            OnCall?.Invoke();
            if (!mail.Deliver)
                return Task.FromResult(false);
            Io++;
            var accepted = Accept && (FailAfter == 0 || Io <= FailAfter);
            return Task.FromResult(accepted);
        }

        public void Reset()
        {
            Calls.Clear();
            Io = 0;
            Accept = true;
            FailAfter = 0;
        }
    }

    private sealed class FakeQuota : IAccountMailQuota
    {
        public int Count { get; set; }
        public bool Fail { get; set; }
        public List<string> Keys { get; } = [];

        public Task<AccountMailSlot> TryReserveAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default)
        {
            if (Fail)
                return Task.FromResult(AccountMailSlot.StoreUnavailable);
            Keys.Add(InMemoryAccountMailQuota.Key(bucket, normalizedEmail));
            if (Count >= 3)
                return Task.FromResult(AccountMailSlot.Exhausted);
            Count++;
            return Task.FromResult(AccountMailSlot.Reserved);
        }

        public Task ReleaseAsync(string bucket, string normalizedEmail, CancellationToken cancellationToken = default)
        {
            if (Count > 0)
                Count--;
            return Task.CompletedTask;
        }
    }
}
