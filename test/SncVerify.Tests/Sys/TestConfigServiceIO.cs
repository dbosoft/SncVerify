using SncVerify.Config;
using SncVerify.Sys;

namespace SncVerify.Tests.Sys;

public class TestConfigServiceIO : ConfigServiceIO
{
    private readonly Dictionary<string, SncVerifyConfig> _store = new();
    private string _configPath = "/test/sncverify.json";

    public SncVerifyConfig? LastWrittenConfig { get; private set; }

    public void SetConfigPath(string path) => _configPath = path;

    public void SetConfig(string path, SncVerifyConfig config) => _store[path] = config;

    public string GetConfigPath() => _configPath;

    public Either<Error, SncVerifyConfig> ReadConfig(string path) =>
        _store.TryGetValue(path, out var config)
            ? config
            : new SncVerifyConfig { Pse = PseConfig.WithDefaults() };

    public Either<Error, Unit> WriteConfig(string path, SncVerifyConfig config)
    {
        _store[path] = config;
        LastWrittenConfig = config;
        return Prelude.unit;
    }
}
