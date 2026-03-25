using System.Text;
using Dbosoft.YaNco;
using Dbosoft.YaNco.Live;
using Dbosoft.YaNco.Traits;
using Dbosoft.YaNco.TypeMapping;
using LanguageExt.Sys.Traits;
using SncVerify.Sys;
using Spectre.Console;

namespace SncVerify.Runtime;

public readonly struct SapRfcRuntime :
    HasAnsiConsole<SapRfcRuntime>,
    HasFile<SapRfcRuntime>,
    HasProcessRunner<SapRfcRuntime>,
    HasConfigService<SapRfcRuntime>,
    HasPseService<SapRfcRuntime>,
    HasSAPRfc<SapRfcRuntime>,
    HasSAPRfcServer<SapRfcRuntime>
{
    private readonly SapRfcRuntimeEnv _env;

    private SapRfcRuntime(SapRfcRuntimeEnv env) =>
        _env = env;

    public static SapRfcRuntime New(IAnsiConsole ansiConsole) =>
        new(new SapRfcRuntimeEnv(
            ansiConsole,
            new SAPRfcRuntimeSettings(null, SAPRfcRuntime.Default.Env.Settings.FieldMapper,
                new RfcRuntimeOptions()),
            new CancellationTokenSource()));

    private SAPRfcRuntimeEnv<SAPRfcRuntimeSettings> RfcEnv =>
        new(_env.CancellationTokenSource, _env.RfcSettings);

    SAPRfcRuntimeEnv<SAPRfcRuntimeSettings> IHasEnvRuntimeSettings.Env =>
        RfcEnv.ToRuntimeSettings();

    public SapRfcRuntime LocalCancel =>
        new(new SapRfcRuntimeEnv(_env.AnsiConsole, _env.RfcSettings, new CancellationTokenSource()));

    public CancellationToken CancellationToken => _env.CancellationTokenSource.Token;
    public CancellationTokenSource CancellationTokenSource => _env.CancellationTokenSource;

    public Eff<SapRfcRuntime, AnsiConsoleIO> AnsiConsoleEff =>
        Eff<SapRfcRuntime, AnsiConsoleIO>(rt => new LiveAnsiConsoleIO(rt._env.AnsiConsole));

    public Encoding Encoding => Encoding.UTF8;

    public Eff<SapRfcRuntime, FileIO> FileEff =>
        SuccessEff(LanguageExt.Sys.Live.FileIO.Default);

    public Eff<SapRfcRuntime, ProcessRunnerIO> ProcessRunnerEff =>
        SuccessEff(LiveProcessRunnerIO.Default);

    public Eff<SapRfcRuntime, ConfigServiceIO> ConfigServiceEff =>
        SuccessEff(LiveConfigServiceIO.Default);

    public Eff<SapRfcRuntime, PseServiceIO> PseServiceEff =>
        SuccessEff(LivePseServiceIO.Default);

    private Option<ILogger> RfcLogger => _env.RfcSettings.Logger == null
        ? Option<ILogger>.None
        : Some(_env.RfcSettings.Logger);

    private SAPRfcDataIO DataIO => _env.RfcSettings.RfcDataIO
        ?? new LiveSAPRfcDataIO(RfcLogger, _env.RfcSettings.FieldMapper, _env.RfcSettings.Options);

    private SAPRfcFunctionIO FunctionIO => _env.RfcSettings.RfcFunctionIO
        ?? new LiveSAPRfcFunctionIO(RfcLogger, DataIO);

    private SAPRfcConnectionIO ConnectionIO => _env.RfcSettings.RfcConnectionIO
        ?? new LiveSAPRfcConnectionIO(RfcLogger);

    public Eff<SapRfcRuntime, Option<ILogger>> RfcLoggerEff =>
        Eff<SapRfcRuntime, Option<ILogger>>(rt => rt.RfcLogger);

    public Eff<SapRfcRuntime, SAPRfcDataIO> RfcDataEff =>
        Eff<SapRfcRuntime, SAPRfcDataIO>(rt => rt.DataIO);

    public Eff<SapRfcRuntime, SAPRfcFunctionIO> RfcFunctionsEff =>
        Eff<SapRfcRuntime, SAPRfcFunctionIO>(rt => rt.FunctionIO);

    public Eff<SapRfcRuntime, IFieldMapper> FieldMapperEff =>
        Eff<SapRfcRuntime, IFieldMapper>(rt => rt._env.RfcSettings.FieldMapper);

    public Eff<SapRfcRuntime, SAPRfcConnectionIO> RfcConnectionEff =>
        Eff<SapRfcRuntime, SAPRfcConnectionIO>(rt => rt.ConnectionIO);

    private SAPRfcServerIO ServerIO => _env.RfcSettings.RfcServerIO
        ?? new LiveSAPRfcServerIO(RfcLogger);

    public Eff<SapRfcRuntime, SAPRfcServerIO> RfcServerEff =>
        Eff<SapRfcRuntime, SAPRfcServerIO>(rt => rt.ServerIO);
}

public class SapRfcRuntimeEnv(
    IAnsiConsole ansiConsole,
    SAPRfcRuntimeSettings rfcSettings,
    CancellationTokenSource cancellationTokenSource)
{
    public IAnsiConsole AnsiConsole { get; } = ansiConsole;
    public SAPRfcRuntimeSettings RfcSettings { get; } = rfcSettings;
    public CancellationTokenSource CancellationTokenSource { get; } = cancellationTokenSource;
}
