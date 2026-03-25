using System.ComponentModel;
using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sap;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Check;

public class CheckSettings : SncVerifyCommandSettings
{
    [CommandOption("--local-only")]
    [Description("Run local checks only, skip SAP connection")]
    public bool LocalOnly { get; set; }
}

public class CheckCommand : AsyncCommand<CheckSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, CheckSettings settings) =>
        await RunHelper.Run(
            CheckLogic.Run<SapRfcRuntime>(settings),
            SapRfcRuntime.New(AnsiConsole.Console));
}

public static class CheckLogic
{
    public static Aff<RT, Unit> Run<RT>(CheckSettings settings)
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasSAPRfc<RT> =>
        from _ in AnsiConsole<RT>.markupLine("[bold]SncVerify Check[/]\n")
        // Local checks
        from _1 in PseService<RT>.checkSapLibraries()
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from _2 in CheckLocalConfig<RT>(config)
        from _3 in CheckPseExists<RT>(config)
        // Remote checks
        from _4 in settings.LocalOnly
            ? AnsiConsole<RT>.markupLine("\n[dim]Skipping remote checks (--local-only).[/]")
            : RunRemoteChecks<RT>(config)
        select unit;

    private static Eff<RT, Unit> CheckLocalConfig<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>
    {
        var table = new Table()
            .AddColumn("Setting")
            .AddColumn("Value")
            .AddColumn("Status");

        AddConfigRow(table, "ASHOST", config.Connection.ASHOST);
        AddConfigRow(table, "SYSNR", config.Connection.SYSNR);
        AddConfigRow(table, "CLIENT", config.Connection.CLIENT);
        AddConfigRow(table, "SNC_MYNAME", config.Snc.SNC_MYNAME);
        AddConfigRow(table, "SNC_PARTNERNAME", config.Snc.SNC_PARTNERNAME);
        AddConfigRow(table, "SNC_QOP", config.Snc.SNC_QOP);

        return
            from _ in AnsiConsole<RT>.markupLine("[bold]Local Configuration[/]")
            from __ in AnsiConsole<RT>.write(table)
            select unit;
    }

    private static void AddConfigRow(Table table, string name, string value) =>
        table.AddRow(
            Markup.Escape(name),
            Markup.Escape(value),
            string.IsNullOrEmpty(value) ? "[red]Not set[/]" : "[green]OK[/]");

    private static Aff<RT, Unit> CheckPseExists<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>, HasPseService<RT> =>
        from io in default(RT).PseServiceEff
        from _ in io.FileExists(config.Pse.Path)
            ? AnsiConsole<RT>.markupLine($"[green]PSE found:[/] {Markup.Escape(config.Pse.Path)}")
            : AnsiConsole<RT>.markupLine($"[red]PSE not found:[/] {Markup.Escape(config.Pse.Path)}\n" +
                "Run 'sncverify setup'.")
        select unit;

    private static Aff<RT, Unit> RunRemoteChecks<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasSAPRfc<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[bold]Remote SAP System Check[/]")
        from user in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("USER (SAP logon user):"))
        from passwd in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("PASSWD:").Secret())
        let connParams = config.Connection.ToDictionary(
            user: user, password: passwd, snc: config.Snc, sncEnabled: false)
        from clientAff in SAPRfc<RT>.buildClient(connParams)
        from _2 in SAPRfc<RT>.useConnection(clientAff, connection =>
            from updated in SapCheckLogic.run<RT>(connection, config)
            select unit)
        select unit;
}
