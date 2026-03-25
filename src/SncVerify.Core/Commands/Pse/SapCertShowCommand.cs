using System.Security.Cryptography.X509Certificates;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Pse;

public class SapCertShowSettings : SncVerifyCommandSettings;

public class SapCertShowCommand : AsyncCommand<SapCertShowSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SapCertShowSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            SapCertShowLogic.Run<SimpleRuntime>(),
            SimpleRuntime.New(AnsiConsole.Console));
}

public static class SapCertShowLogic
{
    public static Aff<RT, Unit> Run<RT>()
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from psePath in !string.IsNullOrEmpty(config.Pse.Path)
            ? SuccessAff(config.Pse.Path)
            : FailAff<string>(Error.New("PSE path not configured. Run 'sncverify setup' first."))
        from certs in PseService<RT>.listTrustedCerts(psePath)
        from _ in AnsiConsole<RT>.markupLine("[bold]SAP Certificates (trusted in local PSE)[/]")
        from __ in string.IsNullOrWhiteSpace(certs)
            ? AnsiConsole<RT>.markupLine("[yellow]No trusted certificates found.[/]\n" +
                "Import SAP's certificate with: sncverify sap_cert import <certfile>")
            : AnsiConsole<RT>.writeLine(certs)
        select unit;
}
