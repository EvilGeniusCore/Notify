using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using Notify.Core.Abstractions;
using Notify.Teams.Models;

namespace Notify.Teams.Services;

/// <summary>
/// Sends messages to a Teams channel via a Power Automate webhook URL.
/// Posts a MessageCard payload to the configured endpoint.
/// </summary>
public class WebhookService : INotificationProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookService> _logger;

    public string ProviderId => "teams";

    public WebhookService(HttpClient httpClient, ILogger<WebhookService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Posts a message to the Teams channel at the webhook URL in <paramref name="credentials"/>.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if either argument is null.</exception>
    /// <exception cref="HttpRequestException">Thrown if the webhook returns a non-success status code.</exception>
    public async Task SendMessageAsync(SendMessageRequest request, WebhookCredentials credentials, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(credentials);

        _logger.LogDebug("Posting MessageCard to webhook. Subject: {Subject}, IsHtml: {IsHtml}.",
            request.Subject ?? "(none)", request.IsHtml);

        var sections = new JsonArray();
        JsonNode bodySection = new JsonObject { ["markdown"] = true, ["text"] = request.Body };
        sections.Add(bodySection);

        var payload = new JsonObject
        {
            ["@type"]    = "MessageCard",
            ["@context"] = "http://schema.org/extensions",
            ["summary"]  = request.Subject ?? Truncate(request.Body, 100),
            ["sections"] = sections,
        };

        if (request.Subject is not null)
            payload["title"] = request.Subject;

        await PostAsync(credentials.WebhookUrl, payload, cancellationToken);
    }

    /// <summary>
    /// Posts a template-driven MessageCard to the webhook.
    /// The template JSON is used as the base payload.
    /// If <paramref name="subjectOverride"/> is provided it replaces <c>title</c> and <c>summary</c>.
    /// If <paramref name="bodyOverride"/> is provided it is appended as a markdown section.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="template"/> or <paramref name="credentials"/> is null.</exception>
    /// <exception cref="HttpRequestException">Thrown if the webhook returns a non-success status code.</exception>
    public async Task SendFromTemplateAsync(
        JsonObject template,
        string?    subjectOverride,
        string?    bodyOverride,
        WebhookCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(credentials);

        _logger.LogDebug("Posting template MessageCard to webhook. Subject override: {Subject}.",
            subjectOverride ?? "(none)");

        if (subjectOverride is not null)
        {
            template["title"]   = subjectOverride;
            template["summary"] = subjectOverride;
        }
        else if (template["summary"] is null)
        {
            template["summary"] = bodyOverride is not null ? Truncate(bodyOverride, 100) : "Notification";
        }

        if (bodyOverride is not null)
        {
            var sections = template["sections"]?.AsArray() ?? [];
            JsonNode section = new JsonObject
            {
                ["markdown"] = true,
                ["text"]     = bodyOverride,
            };
            sections.Add(section);
            template["sections"] = sections;
        }

        await PostAsync(credentials.WebhookUrl, template, cancellationToken);
    }

    private async Task PostAsync(string url, JsonNode payload, CancellationToken cancellationToken)
    {
        var content  = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Webhook returned {StatusCode}: {Body}", (int)response.StatusCode, body);
            throw new HttpRequestException(
                $"Webhook returned {(int)response.StatusCode} {response.ReasonPhrase}.",
                inner: null,
                statusCode: response.StatusCode);
        }

        _logger.LogDebug("Webhook accepted the message.");
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
