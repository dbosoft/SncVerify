using System.Security.Cryptography.X509Certificates;
using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using LanguageExt.Effects.Traits;
using SncVerify.Config;
using SncVerify.Sys;
using Spectre.Console;

namespace SncVerify.Sap;

/// <summary>
/// SAP system SNC check logic. Reusable from setup and check command.
/// Each check step catches its own errors and prints remediation steps.
/// </summary>
public static class SapCheckLogic
{
    public static Aff<RT, SncVerifyConfig> run<RT>(
        IConnection connection, SncVerifyConfig config)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasProcessRunner<RT> =>
        from updatedConfig in readAndApplySncParameters<RT>(connection, config)
        from _2 in (importSapCertificate<RT>(connection, updatedConfig)
            ) | @catch(e =>
                AnsiConsole<RT>.markupLine(
                    $"[yellow]Could not import SAP certificate:[/] {Markup.Escape(e.Message)}\n" +
                    "  Export SAP's certificate from STRUST and run: sncverify sap_cert import <certfile>"))
        from _3 in (checkOurCertTrusted<RT>(connection, updatedConfig)
            ) | @catch(e =>
                AnsiConsole<RT>.markupLine(
                    $"[yellow]Could not verify our certificate in SAP:[/] {Markup.Escape(e.Message)}\n" +
                    "  Import our certificate via STRUST in the SAP system."))
        select updatedConfig;

    private static Aff<RT, SncVerifyConfig> readAndApplySncParameters<RT>(
        IConnection connection, SncVerifyConfig config)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT>,
            HasConfigService<RT>
    {
        return
            from _ in AnsiConsole<RT>.markupLine("\n[dim]Reading SAP SNC parameters...[/]")
            from sncEnable in SapRfcCalls<RT>.readProfileParameter(connection, "snc/enable")
            from sncIdentity in SapRfcCalls<RT>.readProfileParameter(connection, "snc/identity/as")
            from sncLib in SapRfcCalls<RT>.readProfileParameter(connection, "snc/gssapi_lib")
            let sncEnabled = sncEnable == "1"
            from _t in renderParameterTable<RT>(sncEnable, sncIdentity, sncLib)
            let updatedConfig = !string.IsNullOrEmpty(sncIdentity)
                ? config with { Snc = config.Snc with { SNC_PARTNERNAME = sncIdentity } }
                : config
            from _1 in !string.IsNullOrEmpty(sncIdentity)
                ? AnsiConsole<RT>.markupLine(
                    $"[green]SNC_PARTNERNAME set to[/] {Markup.Escape(sncIdentity)}")
                : AnsiConsole<RT>.markupLine(
                    "[yellow]Could not determine SAP SNC identity.[/]")
            from configPath in ConfigService<RT>.getConfigPath()
            from _2 in ConfigService<RT>.writeConfig(configPath, updatedConfig)
            select updatedConfig;
    }

    private static Eff<RT, Unit> renderParameterTable<RT>(
        string sncEnable, string sncIdentity, string sncLib)
        where RT : struct, HasAnsiConsole<RT>
    {
        var table = new Table()
            .AddColumn("Parameter")
            .AddColumn("Value")
            .AddColumn("Status");

        table.AddRow("snc/enable", Markup.Escape(sncEnable),
            sncEnable == "1" ? "[green]OK[/]" : "[red]SNC not enabled[/]");
        table.AddRow("snc/identity/as", Markup.Escape(sncIdentity),
            !string.IsNullOrEmpty(sncIdentity) ? "[green]OK[/]" : "[yellow]Not set[/]");
        table.AddRow("snc/gssapi_lib", Markup.Escape(sncLib),
            !string.IsNullOrEmpty(sncLib) ? "[green]OK[/]" : "[yellow]Not set[/]");

        return AnsiConsole<RT>.write(table);
    }

    private static Aff<RT, Unit> importSapCertificate<RT>(
        IConnection connection, SncVerifyConfig config)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT>,
            HasPseService<RT>, HasProcessRunner<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[dim]Fetching SAP server certificate...[/]")
        from certData in SapRfcCalls<RT>.getOwnCertificate(connection)
        from certPath in writeCert(config, certData, "sap_server.crt")
        from _1 in PseService<RT>.importCert(config.Pse.Path, certPath)
        from _2 in AnsiConsole<RT>.markupLine(
            "[green]SAP server certificate imported into local PSE.[/]")
        select unit;

    private static Aff<RT, Unit> checkOurCertTrusted<RT>(
        IConnection connection, SncVerifyConfig config)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[dim]Checking if our certificate is trusted by SAP...[/]")
        from ourThumbprint in getOurCertThumbprint(config)
        from sapTrustedCerts in SapRfcCalls<RT>.getCertificateList(connection)
        let trustedThumbprints = sapTrustedCerts
            .Map(GetThumbprint)
            .Somes()
        let trusted = trustedThumbprints.Exists(t => t == ourThumbprint)
        from _1 in trusted
            ? AnsiConsole<RT>.markupLine("[green]Our certificate is trusted by SAP.[/]")
            : AnsiConsole<RT>.markupLine(
                "[yellow]Our certificate is NOT yet trusted by SAP.[/]\n" +
                "  Import it via STRUST in the SAP system.")
        select unit;


    private static Eff<string> getOurCertThumbprint(SncVerifyConfig config)
    {
        var certPath = Path.Combine(
            Path.GetDirectoryName(config.Pse.Path) ?? PseConfig.GetDefaultPseDir(),
            "own_cert.crt");

        if (!File.Exists(certPath))
            return FailEff<string>(Error.New(
                $"Own certificate not found at {certPath}. Run 'sncverify own_cert export' first."));

        var certData = File.ReadAllBytes(certPath);
        return GetThumbprint(certData).Match(
            Some: SuccessEff,
            None: () => FailEff<string>(Error.New($"Could not parse certificate at {certPath}")));
    }

    private static Option<string> GetThumbprint(byte[] certData)
    {
        try
        {
            using var cert = X509CertificateLoader.LoadCertificate(certData);
            return cert.Thumbprint;
        }
        catch
        {
            return None;
        }
    }

    private static Eff<string> writeCert(SncVerifyConfig config, byte[] certData, string fileName)
    {
        var certPath = Path.Combine(
            Path.GetDirectoryName(config.Pse.Path) ?? PseConfig.GetDefaultPseDir(),
            fileName);

        var dir = Path.GetDirectoryName(certPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        File.WriteAllBytes(certPath, certData);
        return SuccessEff(certPath);
    }
}
