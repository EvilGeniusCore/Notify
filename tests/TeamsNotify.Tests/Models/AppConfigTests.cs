using TeamsNotify.Models;

namespace TeamsNotify.Tests.Models;

public class AppConfigTests
{
    [Fact]
    public void Validate_AllFieldsPresent_DoesNotThrow()
    {
        var config = new AppConfig
        {
            TenantId     = "tenant-id",
            ClientId     = "client-id",
            ClientSecret = "client-secret"
        };

        var ex = Record.Exception(() => config.Validate());
        Assert.Null(ex);
    }

    [Fact]
    public void Validate_MissingTenantId_ThrowsWithFieldName()
    {
        var config = new AppConfig { ClientId = "c", ClientSecret = "s" };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TEAMS_NOTIFY_TENANT_ID", ex.Message);
    }

    [Fact]
    public void Validate_MissingClientId_ThrowsWithFieldName()
    {
        var config = new AppConfig { TenantId = "t", ClientSecret = "s" };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TEAMS_NOTIFY_CLIENT_ID", ex.Message);
    }

    [Fact]
    public void Validate_MissingClientSecret_ThrowsWithFieldName()
    {
        var config = new AppConfig { TenantId = "t", ClientId = "c" };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TEAMS_NOTIFY_CLIENT_SECRET", ex.Message);
    }

    [Fact]
    public void Validate_MultipleFieldsMissing_ListsAllInMessage()
    {
        var config = new AppConfig();

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TEAMS_NOTIFY_TENANT_ID",     ex.Message);
        Assert.Contains("TEAMS_NOTIFY_CLIENT_ID",     ex.Message);
        Assert.Contains("TEAMS_NOTIFY_CLIENT_SECRET", ex.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhitespaceTenantId_ThrowsWithFieldName(string value)
    {
        var config = new AppConfig { TenantId = value, ClientId = "c", ClientSecret = "s" };

        var ex = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("TEAMS_NOTIFY_TENANT_ID", ex.Message);
    }

    [Fact]
    public void ToCredentials_MapsAllThreeFields()
    {
        var config = new AppConfig
        {
            TenantId     = "t",
            ClientId     = "c",
            ClientSecret = "s"
        };

        var creds = config.ToCredentials();

        Assert.Equal("t", creds.TenantId);
        Assert.Equal("c", creds.ClientId);
        Assert.Equal("s", creds.ClientSecret);
    }
}
