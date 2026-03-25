using SncVerify.Sys;

namespace SncVerify.Tests.Sys;

public class TestProcessRunnerIO : ProcessRunnerIO
{
    private readonly List<(string Executable, string Arguments)> _calls = [];
    private readonly Queue<ProcessRunnerResult> _results = new();

    public IReadOnlyList<(string Executable, string Arguments)> Calls => _calls;

    public void EnqueueResult(ProcessRunnerResult result) => _results.Enqueue(result);

    public void EnqueueSuccess(string output = "") =>
        _results.Enqueue(new ProcessRunnerResult(0, output));

    public void EnqueueFailure(int exitCode = 1, string stdErr = "error") =>
        _results.Enqueue(new ProcessRunnerResult(exitCode, "", stdErr));

    public ValueTask<ProcessRunnerResult> RunProcess(
        string executablePath,
        string arguments,
        string workingDirectory = "")
    {
        _calls.Add((executablePath, arguments));
        var result = _results.Count > 0
            ? _results.Dequeue()
            : new ProcessRunnerResult(0, "");
        return ValueTask.FromResult(result);
    }
}
