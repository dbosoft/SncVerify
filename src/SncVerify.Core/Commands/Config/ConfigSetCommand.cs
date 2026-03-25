using System.ComponentModel;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Config;

public class ConfigSetSettings : SncVerifyCommandSettings
{
    [CommandArgument(0, "<key>")]
    [Description("Configuration key (e.g. snc.snc_myname)")]
    public string Key { get; set; } = "";

    [CommandArgument(1, "<value>")]
    [Description("Value to set")]
    public string Value { get; set; } = "";
}

public class ConfigSetCommand : AsyncCommand<ConfigSetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConfigSetSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            ConfigSetLogic.Run<SimpleRuntime>(settings.Key, settings.Value),
            SimpleRuntime.New(Spectre.Console.AnsiConsole.Console));
}

public static class ConfigSetLogic
{
    public static Aff<RT, Unit> Run<RT>(string key, string value)
        where RT : struct, HasAnsiConsole<RT>, HasConfigService<RT> =>
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from updatedResult in Eff(() => ConfigService.SetValue(config, key, value))
        from updated in updatedResult.Match(
            Right: SuccessAff,
            Left: FailAff<SncVerifyConfig>)
        from _ in ConfigService<RT>.writeConfig(configPath, updated)
        from __ in AnsiConsole<RT>.markupLine(
            $"[green]Set[/] {Spectre.Console.Markup.Escape(key)} = {Spectre.Console.Markup.Escape(value)}")
        select unit;
}
