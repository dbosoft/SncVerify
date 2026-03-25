using System.ComponentModel;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Pse;

public class SapCertImportSettings : SncVerifyCommandSettings
{
    [CommandArgument(0, "<certfile>")]
    [Description("Path to the SAP certificate file to import")]
    public string CertFile { get; set; } = "";
}

public class SapCertImportCommand : AsyncCommand<SapCertImportSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SapCertImportSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            SapCertImportLogic.Run<SimpleRuntime>(settings),
            SimpleRuntime.New(AnsiConsole.Console));
}

public static class SapCertImportLogic
{
    public static Aff<RT, Unit> Run<RT>(SapCertImportSettings settings)
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from psePath in !string.IsNullOrEmpty(config.Pse.Path)
            ? SuccessAff(config.Pse.Path)
            : FailAff<string>(Error.New("PSE path not configured. Run 'sncverify setup' first."))
        from _ in PseService<RT>.importCert(psePath, settings.CertFile)
        from certs in PseService<RT>.listTrustedCerts(psePath)
        from __ in AnsiConsole<RT>.markupLine("[bold]Trusted certificates:[/]")
        from ___ in AnsiConsole<RT>.writeLine(certs)
        select unit;
}
