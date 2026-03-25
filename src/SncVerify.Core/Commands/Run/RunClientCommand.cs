using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sap;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Run;

public class RunClientSettings : SncVerifyCommandSettings;

public class RunClientCommand : AsyncCommand<RunClientSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunClientSettings settings) =>
        await RunHelper.Run(
            RunClientLogic.Run<SapRfcRuntime>(),
            SapRfcRuntime.New(AnsiConsole.Console));
}

public static class RunClientLogic
{
    public static Aff<RT, Unit> Run<RT>()
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasSAPRfc<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from _ in AnsiConsole<RT>.markupLine("[bold]SncVerify — Run Client (SNC)[/]\n")
        let connParams = config.Connection.ToDictionary(
            snc: config.Snc, sncEnabled: true)
        from clientAff in SAPRfc<RT>.buildClient(connParams)
        from result in SAPRfc<RT>.useConnection(clientAff, connection =>
            SapClientLogic.run<RT>(connection))
        from _1 in SapClientLogic.renderResult<RT>(result)
        select unit;
}
