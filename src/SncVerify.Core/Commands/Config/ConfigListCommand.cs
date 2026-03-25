using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Config;

public class ConfigListSettings : SncVerifyCommandSettings;

public class ConfigListCommand : AsyncCommand<ConfigListSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConfigListSettings settings, CancellationToken cancellationToken) =>
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
        from _ in RenderTable<RT>(config)
        select unit;

    private static Eff<RT, Unit> RenderTable<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>
    {
        var table = new Table()
            .AddColumn("Parameter")
            .AddColumn("Value");

        foreach (var key in ConfigService.GetAllKeys())
            ConfigService.GetValue(config, key)
                .IfRight(v => table.AddRow(Markup.Escape(key), Markup.Escape(v)));

        return AnsiConsole<RT>.write(table);
    }
}
