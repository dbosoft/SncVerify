using System.ComponentModel;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Pse;

public class OwnCertExportSettings : SncVerifyCommandSettings
{
    [CommandOption("--output|-o")]
    [Description("Output certificate file path")]
    public string? OutputPath { get; set; }
}

public class OwnCertExportCommand : AsyncCommand<OwnCertExportSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, OwnCertExportSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            OwnCertExportLogic.Run<SimpleRuntime>(settings),
            SimpleRuntime.New(AnsiConsole.Console));
}

public static class OwnCertExportLogic
{
    public static Aff<RT, Unit> Run<RT>(OwnCertExportSettings settings)
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from psePath in !string.IsNullOrEmpty(config.Pse.Path)
            ? SuccessAff(config.Pse.Path)
            : FailAff<string>(Error.New("PSE path not configured. Run 'sncverify setup' first."))
        from defaultCertPath in PseService<RT>.getDefaultCertPath(psePath)
        let certPath = settings.OutputPath ?? defaultCertPath
        from _ in PseService<RT>.exportOwnCert(psePath, certPath)
        from __ in AnsiConsole<RT>.markupLine(
            $"\nImport this certificate in SAP via [bold]STRUST[/].")
        select unit;
}
