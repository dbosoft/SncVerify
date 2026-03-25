namespace SncVerify.Sys;

public static class ProcessRunner<RT>
    where RT : struct, HasProcessRunner<RT>
{
    public static Aff<RT, ProcessRunnerResult> runProcess(
        string executablePath,
        string arguments,
        string workingDirectory = "") =>
        default(RT).ProcessRunnerEff.MapAsync(e => e.RunProcess(
            executablePath, arguments, workingDirectory));
}
