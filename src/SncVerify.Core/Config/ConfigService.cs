namespace SncVerify.Config;

/// <summary>
/// Pure config operations (no I/O). For file I/O use <see cref="Sys.ConfigService{RT}"/>.
/// </summary>
public static class ConfigService
{
    public static Either<Error, string> GetValue(SncVerifyConfig config, string key) =>
        key.ToUpperInvariant() switch
        {
            "ASHOST" => config.Connection.ASHOST,
            "SYSID" => config.Connection.SYSID,
            "SYSNR" => config.Connection.SYSNR,
            "CLIENT" => config.Connection.CLIENT,
            "LANG" => config.Connection.LANG,
            "SAPROUTER" => config.Connection.SAPROUTER,
            "GWHOST" => config.Connection.GWHOST,
            "GWSERV" => config.Connection.GWSERV,
            "PROGRAM_ID" => config.Connection.PROGRAM_ID,
            "SNC_QOP" => config.Snc.SNC_QOP,
            "SNC_MYNAME" => config.Snc.SNC_MYNAME,
            "SNC_PARTNERNAME" => config.Snc.SNC_PARTNERNAME,
            "SNC_SSO" => config.Snc.SNC_SSO,
            "PCS" => config.Snc.PCS,
            _ => Left(Error.New($"Unknown configuration key: {key}")),
        };

    public static Either<Error, SncVerifyConfig> SetValue(SncVerifyConfig config, string key, string value) =>
        key.ToUpperInvariant() switch
        {
            "ASHOST" => config with { Connection = config.Connection with { ASHOST = value } },
            "SYSID" => config with { Connection = config.Connection with { SYSID = value } },
            "SYSNR" => config with { Connection = config.Connection with { SYSNR = value } },
            "CLIENT" => config with { Connection = config.Connection with { CLIENT = value } },
            "LANG" => config with { Connection = config.Connection with { LANG = value } },
            "SAPROUTER" => config with { Connection = config.Connection with { SAPROUTER = value } },
            "GWHOST" => config with { Connection = config.Connection with { GWHOST = value } },
            "GWSERV" => config with { Connection = config.Connection with { GWSERV = value } },
            "PROGRAM_ID" => config with { Connection = config.Connection with { PROGRAM_ID = value } },
            "SNC_QOP" => config with { Snc = config.Snc with { SNC_QOP = value } },
            "SNC_MYNAME" => config with { Snc = config.Snc with { SNC_MYNAME = value } },
            "SNC_PARTNERNAME" => config with { Snc = config.Snc with { SNC_PARTNERNAME = value } },
            "SNC_SSO" => config with { Snc = config.Snc with { SNC_SSO = value } },
            "PCS" => config with { Snc = config.Snc with { PCS = value } },
            _ => Left(Error.New($"Unknown configuration key: {key}")),
        };

    public static Seq<string> GetAllKeys() => Seq(
        "ASHOST", "SYSID", "SYSNR", "CLIENT", "LANG", "SAPROUTER",
        "GWHOST", "GWSERV", "PROGRAM_ID",
        "SNC_QOP", "SNC_MYNAME", "SNC_PARTNERNAME", "SNC_SSO", "PCS");
}
