using Notify.Teams.Internal;

namespace Notify.Teams.Tests.Internal;

public class GuidHelperTests
{
    [Theory]
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8")]  // standard hyphenated
    [InlineData("6BA7B810-9DAD-11D1-80B4-00C04FD430C8")]  // uppercase
    [InlineData("6ba7b8109dad11d180b400c04fd430c8")]       // no hyphens
    [InlineData("{6ba7b810-9dad-11d1-80b4-00c04fd430c8}")] // braces
    public void IsGuid_WithValidGuid_ReturnsTrue(string value)
    {
        Assert.True(GuidHelper.IsGuid(value));
    }

    [Theory]
    [InlineData("DevOps")]
    [InlineData("General")]
    [InlineData("my-team-name")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("6ba7b810-9dad-11d1-80b4")]           // truncated
    [InlineData("6ba7b810-9dad-11d1-80b4-00c04fd430c8-extra")] // too long
    [InlineData("not-a-guid-at-all")]
    public void IsGuid_WithNonGuid_ReturnsFalse(string value)
    {
        Assert.False(GuidHelper.IsGuid(value));
    }
}
