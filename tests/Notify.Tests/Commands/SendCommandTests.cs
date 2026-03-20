using System.CommandLine;
using System.CommandLine.Invocation;
using Notify.Commands;

namespace Notify.Tests.Commands;

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
        var exitCode = await root.InvokeAsync("send");

        Assert.Equal(1, exitCode);
    }

    [Fact]
    public async Task Send_WithDryRun_ReturnsSuccess()
    {
        // dry-run short-circuits before credentials are needed
        var root = BuildRoot();

        var exitCode = await root.InvokeAsync("send --message Hello --dry-run");

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Send_DryRun_WithWebhookOverride_ReturnsSuccess()
    {
        var root = BuildRoot();

        var exitCode = await root.InvokeAsync(
            "send --message Hello --webhook https://example.com/webhook --dry-run");

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task Send_MissingWebhookUrl_WithoutDryRun_ReturnsExitCode5()
    {
        // Ensure webhook URL is not set so Validate() throws → exit code 5
        var previous = Environment.GetEnvironmentVariable("NOTIFY_TEAMS_WEBHOOK_URL");
        Environment.SetEnvironmentVariable("NOTIFY_TEAMS_WEBHOOK_URL", null);

        try
        {
            var root = BuildRoot();
            var exitCode = await root.InvokeAsync("send --message Hello");

            Assert.Equal(5, exitCode);
        }
        finally
        {
            Environment.SetEnvironmentVariable("NOTIFY_TEAMS_WEBHOOK_URL", previous);
        }
    }
}
