namespace Tickflo.CoreTest.Services;

using Moq;
using Tickflo.Core.Data;
using Tickflo.Core.Entities;
using Xunit;

public class EmailTemplateServiceTests
{
    private static EmailTemplateService CreateService(IEmailTemplateRepository? repo = null)
    {
        repo ??= Mock.Of<IEmailTemplateRepository>();
        return new EmailTemplateService(repo);
    }

    [Fact]
    public async Task RenderTemplateAsyncReplacesVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, null, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Welcome John, your email is john@example.com", body);
    }

    [Fact]
    public async Task RenderTemplateAsyncThrowsWhenTemplateNotFound()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.ForgotPassword, null, default))
            .ReturnsAsync((EmailTemplate)null!);

        var svc = CreateService(repo.Object);
        var variables = new Dictionary<string, string>();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RenderTemplateAsync(EmailTemplateType.ForgotPassword, variables));
    }

    [Fact]
    public async Task RenderTemplateAsyncHandlesEmptyVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, null, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables);

        Assert.Equal("Static Subject", subject);
        Assert.Equal("Static body with no variables", body);
    }

    [Fact]
    public async Task RenderTemplateAsyncHandlesUnusedVariables()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, null, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Body text", body);
    }

    [Fact]
    public async Task RenderTemplateAsyncLeavesUnknownPlaceholders()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, null, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables);

        Assert.Equal("Hello John", subject);
        Assert.Equal("Welcome John, code: {{CODE}}", body);
    }

    [Fact]
    public async Task RenderTemplateAsyncWorkspaceSpecificTemplate()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, 5, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables, workspaceId: 5);

        Assert.Equal("Workspace Custom", subject);
        Assert.Equal("Custom template for workspace", body);
    }

    [Fact]
    public async Task RenderTemplateAsyncReplacesMultipleOccurrences()
    {
        var repo = new Mock<IEmailTemplateRepository>();
        repo.Setup(r => r.FindByTypeAsync(EmailTemplateType.EmailConfirmationThankYou, null, default))
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

        var (subject, body) = await svc.RenderTemplateAsync(EmailTemplateType.EmailConfirmationThankYou, variables);

        Assert.Equal("John - John", subject);
        Assert.Equal("Hello John, welcome John!", body);
    }
}
