using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TravelAgency.Shared.Infrastructure.Behaviors;
using TravelAgency.Shared.Infrastructure.Extensions;

namespace TravelAgency.Shared.Infrastructure.Tests.Extensions;

/// <summary>
/// Tests for AUDIT-001: AddSharedMediatRBehaviors moved from Application to Infrastructure.
/// Verifies that ValidationBehavior and LoggingBehavior are registered and run in the pipeline.
/// </summary>
public class SharedInfrastructureExtensionsTests
{
    [Fact]
    public void AddSharedMediatRBehaviors_RegistersValidationAndLoggingBehavior()
    {
        var services = new ServiceCollection();
        services.AddSharedMediatRBehaviors();

        var descriptors = services
            .Where(d => d.ServiceType.IsGenericType && d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))
            .ToList();

        descriptors.Should().HaveCount(2, "LoggingBehavior and ValidationBehavior should be registered");

        var implementationTypes = descriptors
            .Select(d => d.ImplementationType)
            .Where(t => t != null)
            .Select(t => t!.GetGenericTypeDefinition())
            .ToList();

        implementationTypes.Should().Contain(typeof(ValidationBehavior<,>).GetGenericTypeDefinition());
        implementationTypes.Should().Contain(typeof(LoggingBehavior<,>).GetGenericTypeDefinition());
    }

    [Fact]
    public async Task AddSharedMediatRBehaviors_ValidationBehaviorRunsInPipeline_WhenInvalidRequestSent()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SharedInfrastructureExtensionsTests).Assembly));
        services.AddValidatorsFromAssembly(typeof(SharedInfrastructureExtensionsTests).Assembly);
        services.AddSharedMediatRBehaviors();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var act = () => mediator.Send(new PipelineTestRequest(""), CancellationToken.None);

        var exception = await act.Should().ThrowAsync<FluentValidation.ValidationException>();
        exception.Which.Errors.Should().Contain(e => e.PropertyName == nameof(PipelineTestRequest.Value));
    }

    [Fact]
    public async Task AddSharedMediatRBehaviors_ValidationBehaviorAllowsValidRequest_ToReachHandler()
    {
        var services = new ServiceCollection();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(SharedInfrastructureExtensionsTests).Assembly));
        services.AddValidatorsFromAssembly(typeof(SharedInfrastructureExtensionsTests).Assembly);
        services.AddSharedMediatRBehaviors();
        services.AddLogging();

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PipelineTestRequest("valid"), CancellationToken.None);

        result.Value.Should().Be("valid");
    }

    /// <summary>
    /// Request type used for pipeline integration tests. Must be in same assembly as AddValidatorsFromAssembly.
    /// </summary>
    public record PipelineTestRequest(string Value) : IRequest<PipelineTestResponse>;

    public record PipelineTestResponse(string Value);

    public class PipelineTestRequestHandler : IRequestHandler<PipelineTestRequest, PipelineTestResponse>
    {
        public Task<PipelineTestResponse> Handle(PipelineTestRequest request, CancellationToken cancellationToken)
            => Task.FromResult(new PipelineTestResponse(request.Value));
    }

    public class PipelineTestRequestValidator : AbstractValidator<PipelineTestRequest>
    {
        public PipelineTestRequestValidator()
        {
            RuleFor(x => x.Value)
                .NotEmpty()
                .MinimumLength(3)
                .WithMessage("Value must be at least 3 characters.");
        }
    }
}

/// <summary>
/// AUDIT-001: Verifies Application layer has no Shared.Infrastructure dependency (DIP compliance).
/// </summary>
public class ApplicationLayerDependencyTests
{
    [Fact]
    public void ApplicationProjects_ShouldNotReferenceSharedInfrastructure()
    {
        var repoRoot = FindRepoRoot();
        var servicesPath = Path.Combine(repoRoot, "src", "Services");
        if (!Directory.Exists(servicesPath))
            return; // Skip if not in repo layout

        var applicationProjects = Directory
            .GetFiles(servicesPath, "*.csproj", SearchOption.AllDirectories)
            .Where(f => f.Contains("Application", StringComparison.OrdinalIgnoreCase))
            .ToList();

        applicationProjects.Should().NotBeEmpty("At least one Application project should exist");

        foreach (var csprojPath in applicationProjects)
        {
            var content = File.ReadAllText(csprojPath);
            content.Should().NotContain("TravelAgency.Shared.Infrastructure",
                $"Application project {Path.GetFileName(csprojPath)} must not reference Shared.Infrastructure (DIP violation)");
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            var srcPath = Path.Combine(dir, "src");
            if (Directory.Exists(srcPath))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return Directory.GetCurrentDirectory();
    }
}
