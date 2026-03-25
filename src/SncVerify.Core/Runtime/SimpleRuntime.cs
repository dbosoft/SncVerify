using System.Text;
using LanguageExt.Sys.Traits;
using SncVerify.Sys;
using Spectre.Console;

namespace SncVerify.Runtime;

public readonly struct SimpleRuntime :
    HasAnsiConsole<SimpleRuntime>,
    HasFile<SimpleRuntime>,
    HasProcessRunner<SimpleRuntime>,
    HasConfigService<SimpleRuntime>,
    HasPseService<SimpleRuntime>
{
    private readonly SimpleRuntimeEnv _env;

    private SimpleRuntime(SimpleRuntimeEnv env) =>
        _env = env;

    public static SimpleRuntime New(IAnsiConsole ansiConsole) =>
        new(new SimpleRuntimeEnv(ansiConsole, new CancellationTokenSource()));

    public static SimpleRuntime New(IAnsiConsole ansiConsole, CancellationTokenSource cts) =>
        new(new SimpleRuntimeEnv(ansiConsole, cts));

    public SimpleRuntime LocalCancel =>
        new(new SimpleRuntimeEnv(_env.AnsiConsole, new CancellationTokenSource()));

    public CancellationToken CancellationToken => _env.CancellationTokenSource.Token;

    public CancellationTokenSource CancellationTokenSource => _env.CancellationTokenSource;

    public Eff<SimpleRuntime, AnsiConsoleIO> AnsiConsoleEff =>
        Eff<SimpleRuntime, AnsiConsoleIO>(rt => new LiveAnsiConsoleIO(rt._env.AnsiConsole));

    public Encoding Encoding => Encoding.UTF8;

    public Eff<SimpleRuntime, FileIO> FileEff =>
        SuccessEff(LanguageExt.Sys.Live.FileIO.Default);

    public Eff<SimpleRuntime, ProcessRunnerIO> ProcessRunnerEff =>
        SuccessEff(LiveProcessRunnerIO.Default);

    public Eff<SimpleRuntime, ConfigServiceIO> ConfigServiceEff =>
        SuccessEff(LiveConfigServiceIO.Default);

    public Eff<SimpleRuntime, PseServiceIO> PseServiceEff =>
        SuccessEff(LivePseServiceIO.Default);
}

public class SimpleRuntimeEnv(
    IAnsiConsole ansiConsole,
    CancellationTokenSource cancellationTokenSource)
{
    public IAnsiConsole AnsiConsole { get; } = ansiConsole;
    public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
}
