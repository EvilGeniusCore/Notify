namespace Notify.Tests;

public class ExitCodesTests
{
    [Fact]
    public void ExitCodes_HaveExpectedValues()
    {
        Assert.Equal(0, ExitCodes.Success);
        Assert.Equal(1, ExitCodes.GeneralError);
        Assert.Equal(2, ExitCodes.WebhookError);
        Assert.Equal(5, ExitCodes.ConfigurationMissing);
    }

    [Fact]
    public void ExitCodes_AllValuesAreUnique()
    {
        var codes = new[]
        {
            ExitCodes.Success,
            ExitCodes.GeneralError,
            ExitCodes.WebhookError,
            ExitCodes.ConfigurationMissing,
        };

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }
}
