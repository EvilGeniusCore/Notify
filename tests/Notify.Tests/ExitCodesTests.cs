namespace Notify.Tests;

public class ExitCodesTests
{
    [Fact]
    public void ExitCodes_HaveExpectedValues()
    {
        Assert.Equal(0, ExitCodes.Success);
        Assert.Equal(1, ExitCodes.GeneralError);
        Assert.Equal(2, ExitCodes.AuthFailure);
        Assert.Equal(3, ExitCodes.NotFound);
        Assert.Equal(4, ExitCodes.GraphApiError);
        Assert.Equal(5, ExitCodes.ConfigurationMissing);
    }

    [Fact]
    public void ExitCodes_AllValuesAreUnique()
    {
        var codes = new[]
        {
            ExitCodes.Success,
            ExitCodes.GeneralError,
            ExitCodes.AuthFailure,
            ExitCodes.NotFound,
            ExitCodes.GraphApiError,
            ExitCodes.ConfigurationMissing
        };

        Assert.Equal(codes.Length, codes.Distinct().Count());
    }
}
