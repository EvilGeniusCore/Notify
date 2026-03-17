using TeamsNotify.Core.Exceptions;

namespace TeamsNotify.Core.Tests.Exceptions;

public class TeamsNotFoundExceptionTests
{
    [Fact]
    public void Constructor_WithMessage_PreservesMessage()
    {
        var ex = new TeamsNotFoundException("No team found with name 'DevOps'.");

        Assert.Equal("No team found with name 'DevOps'.", ex.Message);
    }

    [Fact]
    public void Constructor_WithMessageAndInner_PreservesBoth()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new TeamsNotFoundException("outer", inner);

        Assert.Equal("outer", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void IsAssignableTo_Exception()
    {
        var ex = new TeamsNotFoundException("test");

        Assert.IsAssignableFrom<Exception>(ex);
    }
}
