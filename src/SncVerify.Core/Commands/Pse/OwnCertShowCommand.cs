using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Pse;

public class OwnCertShowSettings : SncVerifyCommandSettings;

public class OwnCertShowCommand : AsyncCommand<OwnCertShowSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, OwnCertShowSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            OwnCertShowLogic.Run<SimpleRuntime>(),
            SimpleRuntime.New(AnsiConsole.Console));
}

public static class OwnCertShowLogic
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
        from defaultCertPath in PseService<RT>.getDefaultCertPath(psePath)
        from _export in PseService<RT>.exportOwnCert(psePath, defaultCertPath)
        from info in PseService<RT>.getMyName(psePath)
        from _ in AnsiConsole<RT>.markupLine("[bold]Own Certificate[/]")
        from __ in AnsiConsole<RT>.writeLine(info)
        from ___ in AnsiConsole<RT>.markupLine(
            $"\nCertificate file: [blue]{Markup.Escape(defaultCertPath)}[/]")
        select unit;
}
