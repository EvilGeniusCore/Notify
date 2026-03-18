using Microsoft.Extensions.Logging.Abstractions;
using Notify.Teams.Models;
using Notify.Teams.Services;

namespace Notify.Teams.Tests.Services;

public class AuthServiceTests
{
    private static AuthService BuildService(string tenantId, string clientId, string clientSecret)
    {
        var credentials = new TeamsCredentials
        {
            TenantId = tenantId,
            ClientId = clientId,
            ClientSecret = clientSecret
        };
        return new AuthService(credentials, NullLogger<AuthService>.Instance);
    }

    [Fact]
    public void BuildGraphClient_WithValidCredentials_ReturnsClient()
    {
        var service = BuildService("tenant-id", "client-id", "client-secret");

        // Construction must not throw — actual auth is lazy on first Graph call
        var client = service.BuildGraphClient();

        Assert.NotNull(client);
    }

    [Theory]
    [InlineData("", "client-id", "client-secret", nameof(TeamsCredentials.TenantId))]
    [InlineData("   ", "client-id", "client-secret", nameof(TeamsCredentials.TenantId))]
    [InlineData("tenant-id", "", "client-secret", nameof(TeamsCredentials.ClientId))]
    [InlineData("tenant-id", "   ", "client-secret", nameof(TeamsCredentials.ClientId))]
    [InlineData("tenant-id", "client-id", "", nameof(TeamsCredentials.ClientSecret))]
    [InlineData("tenant-id", "client-id", "   ", nameof(TeamsCredentials.ClientSecret))]
    public void BuildGraphClient_WithEmptyCredentialField_ThrowsArgumentException(
        string tenantId, string clientId, string clientSecret, string expectedParamName)
    {
        var service = BuildService(tenantId, clientId, clientSecret);

        var ex = Assert.Throws<ArgumentException>(() => service.BuildGraphClient());
        Assert.Equal(expectedParamName, ex.ParamName);
    }
}
