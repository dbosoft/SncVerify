using System.Text.Json.Serialization;

namespace SncVerify.Config;

public record SncVerifyConfig
{
    [JsonPropertyName("connection")]
    public ConnectionConfig Connection { get; init; } = new();

    [JsonPropertyName("snc")]
    public SncConfig Snc { get; init; } = new();

    [JsonPropertyName("pse")]
    public PseConfig Pse { get; init; } = new();
}

/// <summary>
/// SAP RFC connection parameters. Property names match SAP RFC SDK parameter names.
/// These are passed as-is to the sapnwrfc library.
/// </summary>
public record ConnectionConfig
{
    [JsonPropertyName("ASHOST")]
    public string ASHOST { get; init; } = "";

    [JsonPropertyName("SYSID")]
    public string SYSID { get; init; } = "";

    [JsonPropertyName("SYSNR")]
    public string SYSNR { get; init; } = "00";

    [JsonPropertyName("CLIENT")]
    public string CLIENT { get; init; } = "100";

    [JsonPropertyName("LANG")]
    public string LANG { get; init; } = "EN";

    [JsonPropertyName("SAPROUTER")]
    public string SAPROUTER { get; init; } = "";

    [JsonPropertyName("GWHOST")]
    public string GWHOST { get; init; } = "";

    [JsonPropertyName("GWSERV")]
    public string GWSERV { get; init; } = "";

    [JsonPropertyName("PROGRAM_ID")]
    public string PROGRAM_ID { get; init; } = "SNCVERIFY";

    [JsonPropertyName("REG_COUNT")]
    public string REG_COUNT { get; init; } = "1";

    /// <summary>
    /// Builds the RFC connection parameter dictionary for YaNco.
    /// Includes connection parameters and optionally user/password and SNC settings.
    /// Empty values are excluded.
    /// </summary>
    public Dictionary<string, string> ToDictionary(
        string? user = null,
        string? password = null,
        SncConfig? snc = null,
        bool sncEnabled = false)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ASHOST"] = ASHOST,
            ["SYSNR"] = SYSNR,
            ["CLIENT"] = CLIENT,
            ["LANG"] = LANG,
            ["SAPROUTER"] = SAPROUTER,
            ["SYSID"] = SYSID,
            ["USER"] = user ?? "",
            ["PASSWD"] = password ?? "",
            ["SNC_MODE"] = sncEnabled && snc != null ? "1" : "0",
            ["SNC_QOP"] = snc?.SNC_QOP ?? "",
            ["SNC_MYNAME"] = snc?.SNC_MYNAME ?? "",
            ["SNC_PARTNERNAME"] = snc?.SNC_PARTNERNAME ?? "",
            ["SNC_SSO"] = snc?.SNC_SSO ?? "",
            ["PCS"] = snc?.PCS ?? "2",
        };

        // Gateway params only when SNC is active — gateway rejects non-SNC connections
        if (sncEnabled)
        {
            dict["GWHOST"] = GWHOST;
            dict["GWSERV"] = GWSERV;
            dict["PROGRAM_ID"] = PROGRAM_ID;
            dict["REG_COUNT"] = REG_COUNT;
        }

        return dict
            .Where(kv => !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
    }
}

/// <summary>
/// SNC parameters. Property names match SAP RFC SDK parameter names.
/// </summary>
public record SncConfig
{
    [JsonPropertyName("SNC_QOP")]
    public string SNC_QOP { get; init; } = "3";

    [JsonPropertyName("SNC_MYNAME")]
    public string SNC_MYNAME { get; init; } = "p:CN=SNCVERIFY, O=dbosoft";

    [JsonPropertyName("SNC_PARTNERNAME")]
    public string SNC_PARTNERNAME { get; init; } = "";

    [JsonPropertyName("SNC_SSO")]
    public string SNC_SSO { get; init; } = "1";

    [JsonPropertyName("PCS")]
    public string PCS { get; init; } = "2";
}

public record PseConfig
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = "";

    [JsonPropertyName("secudir")]
    public string SecuDir { get; init; } = "";

    public static PseConfig WithDefaults() => new()
    {
        Path = System.IO.Path.Combine(GetDefaultPseDir(), "SAPSNCS.pse"),
        SecuDir = GetDefaultPseDir(),
    };

    public static string GetDefaultPseDir() =>
        System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows)
            ? System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "sncverify", "sec")
            : System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".sncverify", "sec");
}
