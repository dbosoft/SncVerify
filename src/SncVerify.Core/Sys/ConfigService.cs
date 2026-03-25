using SncVerify.Config;

namespace SncVerify.Sys;

public static class ConfigService<RT>
    where RT : struct, HasConfigService<RT>
{
    public static Eff<RT, string> getConfigPath() =>
        default(RT).ConfigServiceEff.Map(io => io.GetConfigPath());

    public static Eff<RT, SncVerifyConfig> readConfig(string path) =>
        from io in default(RT).ConfigServiceEff
        from result in io.ReadConfig(path).Match(
            Right: SuccessEff,
            Left: FailEff<SncVerifyConfig>)
        select result;

    public static Eff<RT, Unit> writeConfig(string path, SncVerifyConfig config) =>
        from io in default(RT).ConfigServiceEff
        from result in io.WriteConfig(path, config).Match(
            Right: _ => SuccessEff(unit),
            Left: FailEff<Unit>)
        select result;
}
