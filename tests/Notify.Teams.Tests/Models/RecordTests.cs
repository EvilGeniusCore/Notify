using Notify.Teams.Models;

namespace Notify.Teams.Tests.Models;

public class RecordTests
{
    // -------------------------------------------------------------------------
    // WebhookCredentials
    // -------------------------------------------------------------------------

    [Fact]
    public void WebhookCredentials_Properties_RoundTrip()
    {
        var creds = new WebhookCredentials { WebhookUrl = "https://example.com/webhook" };

        Assert.Equal("https://example.com/webhook", creds.WebhookUrl);
    }

    [Fact]
    public void WebhookCredentials_RecordEquality_EqualWhenSameValues()
    {
        var a = new WebhookCredentials { WebhookUrl = "https://example.com/webhook" };
        var b = new WebhookCredentials { WebhookUrl = "https://example.com/webhook" };

        Assert.Equal(a, b);
    }

    [Fact]
    public void WebhookCredentials_RecordEquality_NotEqualWhenDifferentValues()
    {
        var a = new WebhookCredentials { WebhookUrl = "https://example.com/webhook-a" };
        var b = new WebhookCredentials { WebhookUrl = "https://example.com/webhook-b" };

        Assert.NotEqual(a, b);
    }

    // -------------------------------------------------------------------------
    // SendMessageRequest
    // -------------------------------------------------------------------------

    [Fact]
    public void SendMessageRequest_Properties_RoundTrip()
    {
        var request = new SendMessageRequest { Body = "Hello!" };

        Assert.Equal("Hello!", request.Body);
    }

    [Fact]
    public void SendMessageRequest_IsHtml_DefaultsToFalse()
    {
        var request = new SendMessageRequest { Body = "Hello!" };

        Assert.False(request.IsHtml);
    }

    [Fact]
    public void SendMessageRequest_Subject_DefaultsToNull()
    {
        var request = new SendMessageRequest { Body = "Hello!" };

        Assert.Null(request.Subject);
    }

    [Fact]
    public void SendMessageRequest_IsHtml_CanBeSetToTrue()
    {
        var request = new SendMessageRequest { Body = "<b>Hello!</b>", IsHtml = true };

        Assert.True(request.IsHtml);
    }

    [Fact]
    public void SendMessageRequest_Subject_CanBeSet()
    {
        var request = new SendMessageRequest { Body = "Hello!", Subject = "Build Failed" };

        Assert.Equal("Build Failed", request.Subject);
    }

    [Fact]
    public void SendMessageRequest_RecordEquality_EqualWhenSameValues()
    {
        var a = new SendMessageRequest { Body = "Hello!" };
        var b = new SendMessageRequest { Body = "Hello!" };

        Assert.Equal(a, b);
    }
}
