using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;
using AnsiConsole = Spectre.Console.AnsiConsole;

namespace SncVerify.Commands.Run;

public class RunServerSettings : SncVerifyCommandSettings;

public class RunServerCommand : AsyncCommand<RunServerSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, RunServerSettings settings) =>
        await RunHelper.Run(
            RunServerLogic.Run<SapRfcRuntime>(),
            SapRfcRuntime.New(AnsiConsole.Console));
}

public static class RunServerLogic
{
    public static Aff<RT, Unit> Run<RT>()
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT>,
            HasSAPRfc<RT>, HasSAPRfcServer<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from _ in AnsiConsole<RT>.markupLine("[bold]SncVerify — Run Server (SNC)[/]\n")
        from _1 in string.IsNullOrEmpty(config.Connection.GWHOST)
            ? FailAff<Unit>(Error.New(
                "GWHOST not configured. Run 'sncverify setup' with server scenario."))
            : SuccessAff(unit)
        let serverParams = config.Connection.ToDictionary(
            snc: config.Snc, sncEnabled: true)
        let stopSignal = new TaskCompletionSource()
        from _a in AnsiConsole<RT>.markupLine(
            $"\nStarting IDoc Receiver on [bold]{Markup.Escape(config.Connection.GWHOST)}:{Markup.Escape(config.Connection.GWSERV)}[/]" +
            $" with program ID [bold]{Markup.Escape(config.Connection.PROGRAM_ID)}[/]" +
            "\nListening on RFM IDOC_INBOUND_ASYNCHRONOUS. Press any key to stop.\n")
        from serverAff in SAPRfcServer<RT>.buildServer(serverParams,
            c => c
                .WithClientConnection(serverParams,
                    cc => cc.WithFunctionHandler("IDOC_INBOUND_ASYNCHRONOUS",
                        SAPIDocServer<RT>.processInboundIDoc))
                .WithServerStateListener(stateChange =>
                {
                    AnsiConsole.MarkupLine(stateChange.NewState switch
                    {
                        RfcServerState.Running => "[green]Server running.[/]",
                        RfcServerState.Broken => "[red]Server connection broken.[/]",
                        RfcServerState.Starting => "[dim]Server starting...[/]",
                        RfcServerState.Stopped => "[dim]Server stopped.[/]",
                        _ => $"[dim]Server state: {stateChange.OldState} -> {stateChange.NewState}[/]",
                    });
                    if (stateChange.NewState == RfcServerState.Stopped)
                        stopSignal.TrySetResult();
                })
                .WithServerErrorListener((_, errorInfo) =>
                    AnsiConsole.MarkupLine(
                        $"[red]Server error:[/] {Markup.Escape(errorInfo.Message)}")))
        from _2 in SAPRfcServer<RT>.useServer(serverAff, rfcServer =>
            from _wait in waitForKeyOrStop<RT>(stopSignal)
            from _c in SAPRfcServer<RT>.stopServer(rfcServer)
            select unit)
        select unit;

    private static Aff<RT, Unit> waitForKeyOrStop<RT>(TaskCompletionSource stopSignal)
        where RT : struct, HasAnsiConsole<RT> =>
        AffMaybe<RT, Unit>(async _ =>
        {
            var keyTask = Task.Run(async () =>
            {
                while (!Console.KeyAvailable)
                    await Task.Delay(100);
                Console.ReadKey(true);
            });

            await Task.WhenAny(keyTask, stopSignal.Task);
            return unit;
        });
}
