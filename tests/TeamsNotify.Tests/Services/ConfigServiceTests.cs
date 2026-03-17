using TeamsNotify.Services;

namespace TeamsNotify.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    // Env vars set during tests are isolated via this list and cleaned up in Dispose.
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
    public async Task LoadAsync_ReadsEnvVars()
    {
        SetEnv("TEAMS_NOTIFY_TENANT_ID",     "env-tenant");
        SetEnv("TEAMS_NOTIFY_CLIENT_ID",     "env-client");
        SetEnv("TEAMS_NOTIFY_CLIENT_SECRET", "env-secret");

        var config = await new ConfigService().LoadAsync(null);

        Assert.Equal("env-tenant", config.TenantId);
        Assert.Equal("env-client", config.ClientId);
        Assert.Equal("env-secret", config.ClientSecret);
    }

    [Fact]
    public async Task LoadAsync_ReadsDefaultsFromEnvVars()
    {
        SetEnv("TEAMS_NOTIFY_DEFAULT_TEAM",    "env-team");
        SetEnv("TEAMS_NOTIFY_DEFAULT_CHANNEL", "env-channel");

        var config = await new ConfigService().LoadAsync(null);

        Assert.Equal("env-team",    config.DefaultTeam);
        Assert.Equal("env-channel", config.DefaultChannel);
    }

    // -------------------------------------------------------------------------
    // Env-file loading
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_ReadsEnvFile()
    {
        var envFile = await WriteTempFileAsync("""
            TEAMS_NOTIFY_TENANT_ID=file-tenant
            TEAMS_NOTIFY_CLIENT_ID=file-client
            TEAMS_NOTIFY_CLIENT_SECRET=file-secret
            """);

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("file-tenant", config.TenantId);
        Assert.Equal("file-client", config.ClientId);
        Assert.Equal("file-secret", config.ClientSecret);
    }

    [Fact]
    public async Task LoadAsync_EnvFileIgnoresCommentLines()
    {
        var envFile = await WriteTempFileAsync("""
            # this is a comment
            TEAMS_NOTIFY_TENANT_ID=real-tenant
            """);

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("real-tenant", config.TenantId);
    }

    [Fact]
    public async Task LoadAsync_EnvFileIgnoresLinesWithoutEquals()
    {
        var envFile = await WriteTempFileAsync("""
            TEAMS_NOTIFY_TENANT_ID=tenant-value
            INVALID_LINE_NO_EQUALS
            """);

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("tenant-value", config.TenantId);
    }

    // -------------------------------------------------------------------------
    // Resolution order
    // -------------------------------------------------------------------------

    [Fact]
    public async Task LoadAsync_EnvFileOverridesEnvVar()
    {
        SetEnv("TEAMS_NOTIFY_TENANT_ID", "env-tenant");

        var envFile = await WriteTempFileAsync("TEAMS_NOTIFY_TENANT_ID=file-tenant");

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("file-tenant", config.TenantId);
    }

    [Fact]
    public async Task LoadAsync_EnvFileDoesNotClearUnmentionedEnvVars()
    {
        SetEnv("TEAMS_NOTIFY_CLIENT_ID", "env-client");

        // env-file only sets TenantId
        var envFile = await WriteTempFileAsync("TEAMS_NOTIFY_TENANT_ID=file-tenant");

        var config = await new ConfigService().LoadAsync(envFile);

        Assert.Equal("file-tenant", config.TenantId);
        Assert.Equal("env-client",  config.ClientId);
    }

    // -------------------------------------------------------------------------
    // GetConfigFilePath
    // -------------------------------------------------------------------------

    [Fact]
    public void GetConfigFilePath_ContainsTeamsNotifyFolder()
    {
        var path = new ConfigService().GetConfigFilePath();

        Assert.Contains("teams-notify", path);
        Assert.EndsWith("config.json", path);
    }

    // -------------------------------------------------------------------------
    // SaveAsync / LoadAsync round-trip
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaveAsync_ThenLoad_RoundTripsAllFields()
    {
        // Point the service at a temp directory so we don't touch the real config file
        var tempConfig = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "config.json");
        _tempFiles.Add(tempConfig);

        var service = new TestableConfigService(tempConfig);

        var original = new TeamsNotify.Models.AppConfig
        {
            TenantId       = "saved-tenant",
            ClientId       = "saved-client",
            ClientSecret   = "saved-secret",
            DefaultTeam    = "saved-team",
            DefaultChannel = "saved-channel"
        };

        await service.SaveAsync(original);
        var loaded = await service.LoadAsync(null);

        Assert.Equal("saved-tenant",   loaded.TenantId);
        Assert.Equal("saved-client",   loaded.ClientId);
        Assert.Equal("saved-secret",   loaded.ClientSecret);
        Assert.Equal("saved-team",     loaded.DefaultTeam);
        Assert.Equal("saved-channel",  loaded.DefaultChannel);
    }
}

/// <summary>
/// Subclass that redirects the config file to a temp path for testing.
/// </summary>
internal class TestableConfigService(string configFilePath) : ConfigService
{
    public override string GetConfigFilePath() => configFilePath;
}
