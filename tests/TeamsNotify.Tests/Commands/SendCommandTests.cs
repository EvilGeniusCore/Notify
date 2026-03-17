using System.CommandLine;
using System.CommandLine.Invocation;
using TeamsNotify.Commands;

namespace TeamsNotify.Tests.Commands;

public class SendCommandTests
{
    private static Command BuildRoot()
    {
        var envFileOption = new Option<string?>("--env-file");
        var root = new RootCommand();
        root.AddGlobalOption(envFileOption);
        root.AddCommand(SendCommand.Build(envFileOption));
        return root;
    }

    [Fact]
    public async Task Send_NoMessageSourceAndNoStdin_ReturnsExitCode1()
    {
        var root = BuildRoot();

        // No --message, --file, or stdin redirect → should fail with code 1
        var exitCode = await root.InvokeAsync("send --team DevOps --channel Alerts");

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Send_MissingCredentials_WithDryRun_ReturnsSuccess()
    {
        // dry-run short-circuits before credentials are needed
        var root = BuildRoot();

        var exitCode = await root.InvokeAsync(
            "send --team DevOps --channel Alerts --message Hello --dry-run");

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Send_DryRun_NoTeam_ReturnsExitCode5()
    {
        var root = BuildRoot();

        // No team → config missing error before dry-run output
        var exitCode = await root.InvokeAsync("send --message Hello --dry-run");

        Assert.Equal(5, exitCode);
    }

    [Fact]
    public async Task Send_DryRun_NoChannel_ReturnsExitCode5()
    {
        var root = BuildRoot();

        var exitCode = await root.InvokeAsync("send --team DevOps --message Hello --dry-run");

        Assert.Equal(5, exitCode);
    }

    [Fact]
    public async Task Send_MissingCredentials_WithoutDryRun_ReturnsExitCode5()
    {
        // Credentials not set → Validate() throws → exit code 5
        Environment.SetEnvironmentVariable("TEAMS_NOTIFY_TENANT_ID",     null);
        Environment.SetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_ID",     null);
        Environment.SetEnvironmentVariable("TEAMS_NOTIFY_CLIENT_SECRET", null);

        var root = BuildRoot();
        var exitCode = await root.InvokeAsync(
            "send --team DevOps --channel Alerts --message Hello");

        Assert.Equal(5, exitCode);
    }
}
