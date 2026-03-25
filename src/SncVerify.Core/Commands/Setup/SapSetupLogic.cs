using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using LanguageExt.Effects.Traits;
using SncVerify.Config;
using SncVerify.Sap;
using SncVerify.Sys;
using Spectre.Console;

namespace SncVerify.Commands.Setup;

/// <summary>
/// Setup-specific SAP system check: prompts for credentials with retry, then delegates to SapCheckLogic.
/// </summary>
public static class SapSetupLogic
{
    public static Aff<RT, SncVerifyConfig> RunSapSetupCheck<RT>(SncVerifyConfig config)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasProcessRunner<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[bold]SAP System Setup Check[/]")
        from connection in ConnectWithRetry<RT>(config)
        from updatedConfig in SAPRfc<RT>.useConnection(connection, conn =>
            SapCheckLogic.run<RT>(conn, config))
        select updatedConfig;

    private static Aff<RT, Aff<RT, IConnection>> ConnectWithRetry<RT>(
        SncVerifyConfig config, int maxRetries = 3)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT> =>
        TryConnect<RT>(config, 1, maxRetries);

    private static Aff<RT, Aff<RT, IConnection>> TryConnect<RT>(
        SncVerifyConfig config, int attempt, int maxRetries)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT> =>
        from user in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("USER (SAP logon user):"))
        from passwd in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("PASSWD:").Secret())
        let connParams = config.Connection.ToDictionary(
            user: user, password: passwd, snc: config.Snc, sncEnabled: false)
        from clientAff in SAPRfc<RT>.buildClient(connParams)
        from result in (
            from conn in clientAff
            from _ping in SAPRfc<RT>.ping(conn)
            from _ in AnsiConsole<RT>.markupLine("[green]RFC connection successful.[/]")
            select unit
        ).Map(_ => clientAff)
         | @catch(e => attempt < maxRetries
            ? from _ in AnsiConsole<RT>.markupLine(
                  $"[red]Connection failed:[/] {Markup.Escape(e.Message)}")
              from __ in AnsiConsole<RT>.markupLine(
                  $"[yellow]Retry ({attempt}/{maxRetries})...[/]\n")
              from retry in TryConnect<RT>(config, attempt + 1, maxRetries)
              select retry
            : FailAff<Aff<RT, IConnection>>(e))
        select result;
}
