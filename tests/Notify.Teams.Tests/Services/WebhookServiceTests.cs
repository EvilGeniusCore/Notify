using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Notify.Teams.Models;
using Notify.Teams.Services;

namespace Notify.Teams.Tests.Services;

public class WebhookServiceTests
{
    private static readonly WebhookCredentials ValidCredentials =
        new() { WebhookUrl = "https://example.com/webhook" };

    private static (WebhookService Service, StubHttpHandler Handler) Build(
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var handler = new StubHttpHandler(statusCode);
        var service = new WebhookService(new HttpClient(handler), NullLogger<WebhookService>.Instance);
        return (service, handler);
    }

    // -------------------------------------------------------------------------
    // SendMessageAsync — argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendMessageAsync_NullRequest_Throws()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SendMessageAsync(null!, ValidCredentials));
    }

    [Fact]
    public async Task SendMessageAsync_NullCredentials_Throws()
    {
        var (service, _) = Build();
        var request = new SendMessageRequest { Body = "Hello" };

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SendMessageAsync(request, null!));
    }

    // -------------------------------------------------------------------------
    // SendMessageAsync — HTTP behaviour
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendMessageAsync_PostsToWebhookUrl()
    {
        var (service, handler) = Build();
        var request = new SendMessageRequest { Body = "Hello" };

        await service.SendMessageAsync(request, ValidCredentials);

        Assert.Equal("https://example.com/webhook", handler.LastRequestUri?.ToString());
    }

    [Fact]
    public async Task SendMessageAsync_NonSuccessStatus_ThrowsHttpRequestException()
    {
        var (service, _) = Build(HttpStatusCode.BadRequest);
        var request = new SendMessageRequest { Body = "Hello" };

        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SendMessageAsync(request, ValidCredentials));
    }

    [Fact]
    public async Task SendMessageAsync_WithSubject_PayloadContainsTitle()
    {
        var (service, handler) = Build();
        var request = new SendMessageRequest { Body = "Hello", Subject = "My Subject" };

        await service.SendMessageAsync(request, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("My Subject", doc.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_WithSubject_SummaryMatchesSubject()
    {
        var (service, handler) = Build();
        var request = new SendMessageRequest { Body = "Hello", Subject = "My Subject" };

        await service.SendMessageAsync(request, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("My Subject", doc.RootElement.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_WithoutSubject_SummaryTruncatesBody()
    {
        var (service, handler) = Build();
        var request = new SendMessageRequest { Body = "Short body" };

        await service.SendMessageAsync(request, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Short body", doc.RootElement.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SendMessageAsync_BodyInSection_WithMarkdownTrue()
    {
        var (service, handler) = Build();
        var request = new SendMessageRequest { Body = "## Header\n\nBody text" };

        await service.SendMessageAsync(request, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var sections = doc.RootElement.GetProperty("sections");
        Assert.Equal(1, sections.GetArrayLength());
        Assert.Equal("## Header\n\nBody text", sections[0].GetProperty("text").GetString());
        Assert.True(sections[0].GetProperty("markdown").GetBoolean());
    }

    // -------------------------------------------------------------------------
    // SendFromTemplateAsync — argument validation
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendFromTemplateAsync_NullTemplate_Throws()
    {
        var (service, _) = Build();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SendFromTemplateAsync(null!, null, null, ValidCredentials));
    }

    [Fact]
    public async Task SendFromTemplateAsync_NullCredentials_Throws()
    {
        var (service, _) = Build();
        var template = new JsonObject { ["@type"] = "MessageCard" };

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => service.SendFromTemplateAsync(template, null, null, null!));
    }

    // -------------------------------------------------------------------------
    // SendFromTemplateAsync — subject override
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendFromTemplateAsync_SubjectOverride_SetsTitleAndSummary()
    {
        var (service, handler) = Build();
        var template = new JsonObject
        {
            ["@type"]    = "MessageCard",
            ["summary"]  = "Original Summary",
            ["title"]    = "Original Title",
        };

        await service.SendFromTemplateAsync(template, "New Subject", null, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("New Subject", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("New Subject", doc.RootElement.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SendFromTemplateAsync_NoSubjectOverride_PreservesTemplateSummary()
    {
        var (service, handler) = Build();
        var template = new JsonObject
        {
            ["@type"]   = "MessageCard",
            ["summary"] = "Template Summary",
        };

        await service.SendFromTemplateAsync(template, null, null, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("Template Summary", doc.RootElement.GetProperty("summary").GetString());
    }

    [Fact]
    public async Task SendFromTemplateAsync_NoSubjectAndNoTemplateSummary_UsesFallback()
    {
        var (service, handler) = Build();
        var template = new JsonObject { ["@type"] = "MessageCard" };

        await service.SendFromTemplateAsync(template, null, null, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        // Should not throw — summary must be present
        Assert.NotNull(doc.RootElement.GetProperty("summary").GetString());
    }

    // -------------------------------------------------------------------------
    // SendFromTemplateAsync — body override
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SendFromTemplateAsync_BodyOverride_AppendsSection()
    {
        var (service, handler) = Build();
        var template = new JsonObject
        {
            ["@type"]   = "MessageCard",
            ["summary"] = "Summary",
        };

        await service.SendFromTemplateAsync(template, null, "Extra body text", ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var sections = doc.RootElement.GetProperty("sections");
        Assert.Equal(1, sections.GetArrayLength());
        Assert.Equal("Extra body text", sections[0].GetProperty("text").GetString());
        Assert.True(sections[0].GetProperty("markdown").GetBoolean());
    }

    [Fact]
    public async Task SendFromTemplateAsync_BodyOverride_AppendedAfterExistingSections()
    {
        var (service, handler) = Build();
        var template = new JsonObject
        {
            ["@type"]   = "MessageCard",
            ["summary"] = "Summary",
            ["sections"] = new JsonArray
            {
                new JsonObject { ["text"] = "Existing section" },
            },
        };

        await service.SendFromTemplateAsync(template, null, "Appended body", ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var sections = doc.RootElement.GetProperty("sections");
        Assert.Equal(2, sections.GetArrayLength());
        Assert.Equal("Appended body", sections[1].GetProperty("text").GetString());
    }

    [Fact]
    public async Task SendFromTemplateAsync_NoBodyOverride_SectionsUnchanged()
    {
        var (service, handler) = Build();
        var template = new JsonObject
        {
            ["@type"]   = "MessageCard",
            ["summary"] = "Summary",
            ["sections"] = new JsonArray
            {
                new JsonObject { ["text"] = "Only section" },
            },
        };

        await service.SendFromTemplateAsync(template, null, null, ValidCredentials);

        var doc = JsonDocument.Parse(handler.LastRequestBody!);
        var sections = doc.RootElement.GetProperty("sections");
        Assert.Equal(1, sections.GetArrayLength());
    }
}

// -------------------------------------------------------------------------
// Test infrastructure
// -------------------------------------------------------------------------

internal class StubHttpHandler(HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    public Uri?    LastRequestUri  { get; private set; }
    public string? LastRequestBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestUri  = request.RequestUri;
        LastRequestBody = request.Content is not null
            ? await request.Content.ReadAsStringAsync(cancellationToken)
            : null;

        return new HttpResponseMessage(statusCode);
    }
}
