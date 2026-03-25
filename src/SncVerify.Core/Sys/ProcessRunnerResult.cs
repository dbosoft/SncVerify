namespace SncVerify.Sys;

public class ProcessRunnerResult(int exitCode, string output, string stdErr = "")
{
    public int ExitCode { get; } = exitCode;
    public string Output { get; } = output;
    public string StdErr { get; } = stdErr;
}
