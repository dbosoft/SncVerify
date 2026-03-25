using System.Text.Json;
using SncVerify.Config;

namespace SncVerify.Sys;

public interface ConfigServiceIO
{
    string GetConfigPath();
    Either<Error, SncVerifyConfig> ReadConfig(string path);
    Either<Error, Unit> WriteConfig(string path, SncVerifyConfig config);
}

public readonly struct LiveConfigServiceIO : ConfigServiceIO
{
    public const string DefaultConfigFileName = "sncverify.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
    };

    public static readonly ConfigServiceIO Default = new LiveConfigServiceIO();

    public string GetConfigPath() =>
        Path.Combine(Config.PseConfig.GetDefaultPseDir(), DefaultConfigFileName);

    public Either<Error, SncVerifyConfig> ReadConfig(string path)
    {
        if (!File.Exists(path))
            return new SncVerifyConfig { Pse = PseConfig.WithDefaults() };

        try
        {
            var json = File.ReadAllText(path);
            var config = JsonSerializer.Deserialize<SncVerifyConfig>(json, JsonOptions);
            return config ?? new SncVerifyConfig { Pse = PseConfig.WithDefaults() };
        }
        catch (JsonException ex)
        {
            return Error.New($"Invalid configuration file: {ex.Message}");
        }
    }

    public Either<Error, Unit> WriteConfig(string path, SncVerifyConfig config)
    {
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(config, JsonOptions);
            File.WriteAllText(path, json);
            return unit;
        }
        catch (Exception ex)
        {
            return Error.New($"Failed to write configuration: {ex.Message}");
        }
    }
}
