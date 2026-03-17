using TeamsNotify.Core.Models;

namespace TeamsNotify.Core.Tests.Models;

public class RecordTests
{
    // -------------------------------------------------------------------------
    // TeamsCredentials
    // -------------------------------------------------------------------------

    [Fact]
    public void TeamsCredentials_Properties_RoundTrip()
    {
        var creds = new TeamsCredentials
        {
            TenantId = "tenant-id",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        };

        Assert.Equal("tenant-id", creds.TenantId);
        Assert.Equal("client-id", creds.ClientId);
        Assert.Equal("client-secret", creds.ClientSecret);
    }

    [Fact]
    public void TeamsCredentials_RecordEquality_EqualWhenSameValues()
    {
        var a = new TeamsCredentials { TenantId = "t", ClientId = "c", ClientSecret = "s" };
        var b = new TeamsCredentials { TenantId = "t", ClientId = "c", ClientSecret = "s" };

        Assert.Equal(a, b);
    }

    [Fact]
    public void TeamsCredentials_RecordEquality_NotEqualWhenDifferentValues()
    {
        var a = new TeamsCredentials { TenantId = "t1", ClientId = "c", ClientSecret = "s" };
        var b = new TeamsCredentials { TenantId = "t2", ClientId = "c", ClientSecret = "s" };

        Assert.NotEqual(a, b);
    }

    // -------------------------------------------------------------------------
    // SendMessageRequest
    // -------------------------------------------------------------------------

    [Fact]
    public void SendMessageRequest_Properties_RoundTrip()
    {
        var request = new SendMessageRequest
        {
            TeamId = "team-id",
            ChannelId = "channel-id",
            Body = "Hello!"
        };

        Assert.Equal("team-id", request.TeamId);
        Assert.Equal("channel-id", request.ChannelId);
        Assert.Equal("Hello!", request.Body);
    }

    [Fact]
    public void SendMessageRequest_IsHtml_DefaultsToFalse()
    {
        var request = new SendMessageRequest
        {
            TeamId = "team-id",
            ChannelId = "channel-id",
            Body = "Hello!"
        };

        Assert.False(request.IsHtml);
    }

    [Fact]
    public void SendMessageRequest_Subject_DefaultsToNull()
    {
        var request = new SendMessageRequest
        {
            TeamId = "team-id",
            ChannelId = "channel-id",
            Body = "Hello!"
        };

        Assert.Null(request.Subject);
    }

    [Fact]
    public void SendMessageRequest_IsHtml_CanBeSetToTrue()
    {
        var request = new SendMessageRequest
        {
            TeamId = "team-id",
            ChannelId = "channel-id",
            Body = "<b>Hello!</b>",
            IsHtml = true
        };

        Assert.True(request.IsHtml);
    }

    [Fact]
    public void SendMessageRequest_Subject_CanBeSet()
    {
        var request = new SendMessageRequest
        {
            TeamId = "team-id",
            ChannelId = "channel-id",
            Body = "Hello!",
            Subject = "Build Failed"
        };

        Assert.Equal("Build Failed", request.Subject);
    }

    [Fact]
    public void SendMessageRequest_RecordEquality_EqualWhenSameValues()
    {
        var a = new SendMessageRequest { TeamId = "t", ChannelId = "c", Body = "b" };
        var b = new SendMessageRequest { TeamId = "t", ChannelId = "c", Body = "b" };

        Assert.Equal(a, b);
    }

    // -------------------------------------------------------------------------
    // TeamInfo / ChannelInfo
    // -------------------------------------------------------------------------

    [Fact]
    public void TeamInfo_Properties_RoundTrip()
    {
        var team = new TeamInfo("team-id", "DevOps");

        Assert.Equal("team-id", team.Id);
        Assert.Equal("DevOps", team.DisplayName);
    }

    [Fact]
    public void TeamInfo_RecordEquality_EqualWhenSameValues()
    {
        var a = new TeamInfo("id", "DevOps");
        var b = new TeamInfo("id", "DevOps");

        Assert.Equal(a, b);
    }

    [Fact]
    public void ChannelInfo_Properties_RoundTrip()
    {
        var channel = new ChannelInfo("channel-id", "Alerts", "standard");

        Assert.Equal("channel-id", channel.Id);
        Assert.Equal("Alerts", channel.DisplayName);
        Assert.Equal("standard", channel.MembershipType);
    }

    [Fact]
    public void ChannelInfo_RecordEquality_EqualWhenSameValues()
    {
        var a = new ChannelInfo("id", "Alerts", "standard");
        var b = new ChannelInfo("id", "Alerts", "standard");

        Assert.Equal(a, b);
    }
}
