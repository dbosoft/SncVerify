using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Config;

public class ConfigListSettings : SncVerifyCommandSettings;

public class ConfigListCommand : AsyncCommand<ConfigListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConfigListSettings settings) =>
        await RunHelper.Run(
            ConfigListLogic.Run<SimpleRuntime>(),
            SimpleRuntime.New(Spectre.Console.AnsiConsole.Console));
}

public static class ConfigListLogic
{
    public static Aff<RT, Unit> Run<RT>()
        where RT : struct, HasAnsiConsole<RT>, HasConfigService<RT> =>
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from _ in RenderTree<RT>(config)
        select unit;

    private static Eff<RT, Unit> RenderTree<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>
    {
        var tree = new Tree("[bold]sncverify.json[/]");

        var connectionNode = tree.AddNode("[blue]connection[/]");
        connectionNode.AddNode($"ASHOST: {Markup.Escape(config.Connection.ASHOST)}");
        connectionNode.AddNode($"SYSID: {Markup.Escape(config.Connection.SYSID)}");
        connectionNode.AddNode($"SYSNR: {Markup.Escape(config.Connection.SYSNR)}");
        connectionNode.AddNode($"CLIENT: {Markup.Escape(config.Connection.CLIENT)}");
        connectionNode.AddNode($"LANG: {Markup.Escape(config.Connection.LANG)}");
        connectionNode.AddNode($"SAPROUTER: {Markup.Escape(config.Connection.SAPROUTER)}");
        connectionNode.AddNode($"GWHOST: {Markup.Escape(config.Connection.GWHOST)}");
        connectionNode.AddNode($"GWSERV: {Markup.Escape(config.Connection.GWSERV)}");
        connectionNode.AddNode($"PROGRAM_ID: {Markup.Escape(config.Connection.PROGRAM_ID)}");
        connectionNode.AddNode($"REG_COUNT: {Markup.Escape(config.Connection.REG_COUNT)}");

        var sncNode = tree.AddNode("[blue]snc[/]");
        sncNode.AddNode($"SNC_QOP: {Markup.Escape(config.Snc.SNC_QOP)}");
        sncNode.AddNode($"SNC_MYNAME: {Markup.Escape(config.Snc.SNC_MYNAME)}");
        sncNode.AddNode($"SNC_PARTNERNAME: {Markup.Escape(config.Snc.SNC_PARTNERNAME)}");
        sncNode.AddNode($"SNC_SSO: {Markup.Escape(config.Snc.SNC_SSO)}");
        sncNode.AddNode($"PCS: {Markup.Escape(config.Snc.PCS)}");

        return AnsiConsole<RT>.write(tree);
    }
}
