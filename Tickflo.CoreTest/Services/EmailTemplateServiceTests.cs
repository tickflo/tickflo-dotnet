using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Tickflo.Core.Services.Email;
using Xunit;

namespace Tickflo.CoreTest.Services;

public class EmailTemplateServiceTests
{
    private static IEmailTemplateService CreateService(IEmailTemplateRepository? repo = null)
    {
        repo ??= Mock.Of<IEmailTemplateRepository>();
        return new EmailTemplateService(repo);
    }

    [Fact]
    public async Task RenderTemplateAsync_ReplacesVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, null, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 1,
                Subject = "Hello {{NAME}}",
                Body = "Welcome {{NAME}}, your email is {{EMAIL}}"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>
        {
            { "NAME", "John" },
            { "EMAIL", "john@example.com" }
        };

        var (subject, body) = await svc.RenderTemplateAsync(1, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Welcome John, your email is john@example.com", body);
    }

    [Fact]
    public async Task RenderTemplateAsync_ThrowsWhenTemplateNotFound()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(99, null, default))
            .ReturnsAsync((EmailTemplate)null!);

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RenderTemplateAsync(99, variables));
    }

    [Fact]
    public async Task RenderTemplateAsync_HandlesEmptyVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, null, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 1,
                Subject = "Static Subject",
                Body = "Static body with no variables"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>();

        var (subject, body) = await svc.RenderTemplateAsync(1, variables);

        Assert.Equal("Static Subject", subject);
        Assert.Equal("Static body with no variables", body);
    }

    [Fact]
    public async Task RenderTemplateAsync_HandlesUnusedVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, null, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 1,
                Subject = "Hello {{NAME}}",
                Body = "Body text"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>
        {
            { "NAME", "John" },
            { "UNUSED", "Value" }
        };

        var (subject, body) = await svc.RenderTemplateAsync(1, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Body text", body);
    }

    [Fact]
    public async Task RenderTemplateAsync_LeavesUnknownPlaceholders()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, null, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 1,
                Subject = "Hello {{NAME}}",
                Body = "Welcome {{NAME}}, code: {{CODE}}"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>
        {
            { "NAME", "John" }
        };

        var (subject, body) = await svc.RenderTemplateAsync(1, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Welcome John, code: {{CODE}}", body);
    }

    [Fact]
    public async Task RenderTemplateAsync_WorkspaceSpecificTemplate()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, 5, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 2,
                Subject = "Workspace Custom",
                Body = "Custom template for workspace"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>();

        var (subject, body) = await svc.RenderTemplateAsync(1, variables, workspaceId: 5);

        Assert.Equal("Workspace Custom", subject);
        Assert.Equal("Custom template for workspace", body);
    }

    [Fact]
    public async Task RenderTemplateAsync_ReplacesMultipleOccurrences()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(1, null, default))
            .ReturnsAsync(new EmailTemplate
            {
                Id = 1,
                TemplateTypeId = 1,
                Version = 1,
                Subject = "{{NAME}} - {{NAME}}",
                Body = "Hello {{NAME}}, welcome {{NAME}}!"
            });

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>
        {
            { "NAME", "John" }
        };

        var (subject, body) = await svc.RenderTemplateAsync(1, variables);

        Assert.Equal("John - John", subject);
        Assert.Equal("Hello John, welcome John!", body);
    }
}
