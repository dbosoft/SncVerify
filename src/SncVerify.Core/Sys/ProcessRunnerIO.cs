using System.Diagnostics;

namespace SncVerify.Sys;

public interface ProcessRunnerIO
{
    ValueTask<ProcessRunnerResult> RunProcess(
        string executablePath,
        string arguments,
        string workingDirectory = "");
}

public readonly struct LiveProcessRunnerIO : ProcessRunnerIO
{
    public static readonly ProcessRunnerIO Default = new LiveProcessRunnerIO();

    public async ValueTask<ProcessRunnerResult> RunProcess(
        string executablePath,
        string arguments,
        string workingDirectory = "")
    {
        var processStartInfo = new ProcessStartInfo(executablePath, arguments)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
        };

        using var process = Process.Start(processStartInfo);
        if (process is null)
            return new ProcessRunnerResult(-1, "", "The process could not be started");

        var outputs = await Task.WhenAll(
            process.StandardOutput.ReadToEndAsync(),
            process.StandardError.ReadToEndAsync());

        await process.WaitForExitAsync().ConfigureAwait(false);

        return new ProcessRunnerResult(process.ExitCode, outputs[0], outputs[1]);
    }
}
