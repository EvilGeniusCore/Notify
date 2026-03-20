using Notify.Services;

namespace Notify.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly List<string> _envVarsSet = [];
    private readonly List<string> _tempFiles  = [];

    private void SetEnv(string key, string value)
    {
        _envVarsSet.Add(key);
        Environment.SetEnvironmentVariable(key, value);
    }

    private async Task<string> WriteTempFileAsync(string content)
    {
        var path = Path.GetTempFileName();
        await File.WriteAllTextAsync(path, content);
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose()
    {
        foreach (var key in _envVarsSet)
            Environment.SetEnvironmentVariable(key, null);
        foreach (var path in _tempFiles)
            if (File.Exists(path)) File.Delete(path);
    }

    // -------------------------------------------------------------------------
    // Environment variable loading
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReadsWebhookUrlFromEnvVar()
    {
        SetEnv("NOTIFY_TEAMS_WEBHOOK_URL", "https://env.example.com/webhook");

        var config = await new ConfigService().LoadAsync(envFilePath: "nonexistent.env");

        Assert.Equal("https://env.example.com/webhook", config.WebhookUrl);
    }

    // -------------------------------------------------------------------------
    // Env-file loading
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReadsWebhookUrlFromEnvFile()
    {
        var envFile = await WriteTempFileAsync(
            "NOTIFY_TEAMS_WEBHOOK_URL=https://file.example.com/webhook");

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("https://file.example.com/webhook", config.WebhookUrl);
    }

    [Fact]
    public async Task LoadAsync_EnvFileIgnoresCommentLines()
    {
        var envFile = await WriteTempFileAsync("""
            # this is a comment
            NOTIFY_TEAMS_WEBHOOK_URL=https://file.example.com/webhook
            """);

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("https://file.example.com/webhook", config.WebhookUrl);
    }

    [Fact]
    public async Task LoadAsync_EnvFileIgnoresLinesWithoutEquals()
    {
        var envFile = await WriteTempFileAsync("""
            NOTIFY_TEAMS_WEBHOOK_URL=https://file.example.com/webhook
            INVALID_LINE_NO_EQUALS
            """);

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("https://file.example.com/webhook", config.WebhookUrl);
    }

    // -------------------------------------------------------------------------
    // Resolution order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_EnvFileOverridesEnvVar()
    {
        SetEnv("NOTIFY_TEAMS_WEBHOOK_URL", "https://env.example.com/webhook");
        var envFile = await WriteTempFileAsync(
            "NOTIFY_TEAMS_WEBHOOK_URL=https://file.example.com/webhook");

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("https://file.example.com/webhook", config.WebhookUrl);
    }

    // -------------------------------------------------------------------------
    // GetConfigFilePath
    // -------------------------------------------------------------------------

    [Fact]
    public void GetConfigFilePath_ContainsNotifyFolder()
    {
        var path = new ConfigService().GetConfigFilePath();

        Assert.Contains("notify", path);
        Assert.EndsWith("config.json", path);
    }

    // -------------------------------------------------------------------------
    // SaveAsync / LoadAsync round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsWebhookUrl()
    {
        var tempConfig = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");
        _tempFiles.Add(tempConfig);

        var service  = new TestableConfigService(tempConfig);
        var original = new Notify.Models.AppConfig { WebhookUrl = "https://saved.example.com/webhook" };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync(envFilePath: "nonexistent.env");

        Assert.Equal("https://saved.example.com/webhook", loaded.WebhookUrl);
    }
}

internal class TestableConfigService(string configFilePath) : ConfigService
{
    public override string GetConfigFilePath() => configFilePath;
}
