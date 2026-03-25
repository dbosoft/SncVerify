using System.ComponentModel;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Config;

public class ConfigGetSettings : SncVerifyCommandSettings
{
    [CommandArgument(0, "<key>")]
    [Description("Configuration key (e.g. snc.snc_myname)")]
    public string Key { get; set; } = "";
}

public class ConfigGetCommand : AsyncCommand<ConfigGetSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, ConfigGetSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            ConfigGetLogic.Run<SimpleRuntime>(settings.Key),
            SimpleRuntime.New(Spectre.Console.AnsiConsole.Console));
}

public static class ConfigGetLogic
{
    public static Aff<RT, Unit> Run<RT>(string key)
        where RT : struct, HasAnsiConsole<RT>, HasConfigService<RT> =>
        from configPath in ConfigService<RT>.getConfigPath()
        from config in ConfigService<RT>.readConfig(configPath)
        from valueResult in Eff(() => ConfigService.GetValue(config, key))
        from value in valueResult.Match(
            Right: SuccessAff,
            Left: FailAff<string>)
        from _ in AnsiConsole<RT>.writeLine(value)
        select unit;
}
