using System.Text;
using LanguageExt;
using LanguageExt.Sys.Traits;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Testing;

using static LanguageExt.Prelude;

namespace SncVerify.Tests.Sys;

public readonly struct TestRuntime :
    HasAnsiConsole<TestRuntime>,
    HasFile<TestRuntime>,
    HasProcessRunner<TestRuntime>,
    HasConfigService<TestRuntime>,
    HasPseService<TestRuntime>
{
    private readonly TestRuntimeEnv _env;

    private TestRuntime(TestRuntimeEnv env) =>
        _env = env;

    public static TestRuntime New(
        TestConsole? console = null,
        TestProcessRunnerIO? processRunner = null,
        TestConfigServiceIO? configService = null,
        TestPseServiceIO? pseService = null) =>
        new(new TestRuntimeEnv(
            console ?? new TestConsole(),
            processRunner ?? new TestProcessRunnerIO(),
            configService ?? new TestConfigServiceIO(),
            pseService ?? new TestPseServiceIO(),
            new CancellationTokenSource()));

    public TestRuntime LocalCancel =>
        new(new TestRuntimeEnv(
            _env.Console, _env.ProcessRunner, _env.ConfigService,
            _env.PseService, new CancellationTokenSource()));

    public CancellationToken CancellationToken => _env.CancellationTokenSource.Token;

    public CancellationTokenSource CancellationTokenSource => _env.CancellationTokenSource;

    public Eff<TestRuntime, AnsiConsoleIO> AnsiConsoleEff =>
        Eff<TestRuntime, AnsiConsoleIO>(rt => new LiveAnsiConsoleIO(rt._env.Console));

    public Encoding Encoding => Encoding.UTF8;

    public Eff<TestRuntime, FileIO> FileEff =>
        SuccessEff(LanguageExt.Sys.Live.FileIO.Default);

    public Eff<TestRuntime, ProcessRunnerIO> ProcessRunnerEff =>
        Eff<TestRuntime, ProcessRunnerIO>(rt => rt._env.ProcessRunner);

    public Eff<TestRuntime, ConfigServiceIO> ConfigServiceEff =>
        Eff<TestRuntime, ConfigServiceIO>(rt => rt._env.ConfigService);

    public Eff<TestRuntime, PseServiceIO> PseServiceEff =>
        Eff<TestRuntime, PseServiceIO>(rt => rt._env.PseService);

    public TestRuntimeEnv Env => _env;
}

public class TestRuntimeEnv(
    TestConsole console,
    TestProcessRunnerIO processRunner,
    TestConfigServiceIO configService,
    TestPseServiceIO pseService,
    CancellationTokenSource cancellationTokenSource)
{
    public TestConsole Console { get; } = console;
    public TestProcessRunnerIO ProcessRunner { get; } = processRunner;
    public TestConfigServiceIO ConfigService { get; } = configService;
    public TestPseServiceIO PseService { get; } = pseService;
    public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
}
