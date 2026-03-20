using Notify.Models;

namespace Notify.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void Validate_WithWebhookUrl_DoesNotThrow()
    {
        var config = new AppConfig { WebhookUrl = "https://example.com/webhook" };

        var ex = Record.Exception(() => config.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_NullWebhookUrl_Throws()
    {
        var config = new AppConfig();

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhitespaceWebhookUrl_Throws(string value)
    {
        var config = new AppConfig { WebhookUrl = value };

        Assert.Throws<InvalidOperationException>(() => config.Validate());
    }

    [Fact]
    public void Validate_ErrorMessage_ContainsEnvVarName()
    {
        var config = new AppConfig();

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("NOTIFY_TEAMS_WEBHOOK_URL", ex.Message);
    }

    [Fact]
    public void ToCredentials_MapsWebhookUrl()
    {
        var config = new AppConfig { WebhookUrl = "https://example.com/webhook" };

        var creds = config.ToCredentials();

        Assert.Equal("https://example.com/webhook", creds.WebhookUrl);
    }
}
