using Dbosoft.YaNco.Traits;
using SncVerify.Config;
using SncVerify.Runtime;
using SncVerify.Sys;
using Spectre.Console;
using Spectre.Console.Cli;

namespace SncVerify.Commands.Setup;

public class SetupSettings : SncVerifyCommandSettings;

public class SetupCommand : AsyncCommand<SetupSettings>
{
    public override async Task<int> ExecuteAsync(CommandContext context, SetupSettings settings, CancellationToken cancellationToken) =>
        await RunHelper.Run(
            SetupLogic.RunSetup<SapRfcRuntime>(),
            SapRfcRuntime.New(AnsiConsole.Console));
}

public static class SetupLogic
{
    private const string ScenarioClient = "Client only (RFC client with SNC)";
    private const string ScenarioServer = "Server (RFC server registration with SNC, includes client)";

    public static Aff<RT, Unit> RunSetup<RT>()
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT>, HasSAPRfc<RT> =>
        from _check in PseService<RT>.checkSapLibraries()
        // Load existing config for defaults
        from configPath in ConfigService<RT>.getConfigPath()
        from existing in ConfigService<RT>.readConfig(configPath)
        from _ in AnsiConsole<RT>.markupLine("\n[bold]SncVerify Setup Wizard[/]\n")
        // Step 1: Scenario selection
        from scenario in AnsiConsole<RT>.prompt(
            new SelectionPrompt<string>()
                .Title("What do you want to verify?")
                .AddChoices(ScenarioClient, ScenarioServer))
        let needsServer = scenario == ScenarioServer
        // Step 2: Connection settings
        from connection in PromptConnectionSettings<RT>(needsServer, existing.Connection)
        // Step 3: SNC settings
        from snc in PromptSncSettings<RT>(existing.Snc)
        // Step 4: Build config and save before PSE setup
        let config = new SncVerifyConfig
        {
            Connection = connection,
            Snc = snc,
            Pse = PseConfig.WithDefaults(),
        }
        from _3 in ConfigService<RT>.writeConfig(configPath, config)
        from _4 in AnsiConsole<RT>.markupLine(
            $"[green]Configuration saved to[/] {Markup.Escape(configPath)}")
        // Step 5: PSE setup
        from _5 in AutoPseSetup<RT>(config)
        // Step 6: SAP system setup check
        from doSapCheck in AnsiConsole<RT>.confirm(
            "Connect to SAP to verify SNC settings and exchange certificates?", true)
        from _6 in doSapCheck
            ? (from updated in SapSetupLogic.RunSapSetupCheck<RT>(config)
               from _s in AnsiConsole<RT>.markupLine("\n[green]SAP system setup check completed.[/]")
               select unit
              ) | @catch(e =>
                AnsiConsole<RT>.markupLine(
                    $"\n[red]SAP connection failed after retries:[/] {Markup.Escape(e.Message)}"))
            : PromptPartnerNameIfMissing<RT>(config)
        // Step 7: Next steps based on scenario
        from _7 in ShowNextSteps<RT>(needsServer, config)
        select unit;

    private static Aff<RT, ConnectionConfig> PromptConnectionSettings<RT>(
        bool needsServer, ConnectionConfig defaults)
        where RT : struct, HasAnsiConsole<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[bold]SAP Connection Settings[/]")
        from ashost in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("ASHOST (application server hostname):")
                .DefaultValueIfNotEmpty(defaults.ASHOST))
        from sysid in needsServer
            ? AnsiConsole<RT>.prompt(
                new TextPrompt<string>("SYSID (system ID):")
                    .DefaultValueIfNotEmpty(defaults.SYSID))
            : SuccessAff(defaults.SYSID)
        from sysnr in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("SYSNR (system number):")
                .DefaultValue(defaults.SYSNR))
        from client in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("CLIENT:")
                .DefaultValue(defaults.CLIENT))
        from lang in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("LANG:")
                .DefaultValue(defaults.LANG))
        from saprouter in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("SAPROUTER (optional, e.g. /H/saprouter/S/3299/H/):")
                .DefaultValue(defaults.SAPROUTER).AllowEmpty())
        from gwhost in needsServer
            ? AnsiConsole<RT>.prompt(
                new TextPrompt<string>("GWHOST (gateway host):")
                    .DefaultValue(!string.IsNullOrEmpty(defaults.GWHOST) ? defaults.GWHOST : ashost))
            : SuccessAff(defaults.GWHOST)
        let defaultGwserv = !string.IsNullOrEmpty(defaults.GWSERV) ? defaults.GWSERV : $"48{sysnr}"
        from gwserv in needsServer
            ? AnsiConsole<RT>.prompt(
                new TextPrompt<string>("GWSERV (gateway service):")
                    .DefaultValue(defaultGwserv))
            : SuccessAff(defaults.GWSERV)
        from programId in needsServer
            ? AnsiConsole<RT>.prompt(
                new TextPrompt<string>("PROGRAM_ID:")
                    .DefaultValue(defaults.PROGRAM_ID))
            : SuccessAff(defaults.PROGRAM_ID)
        select new ConnectionConfig
        {
            ASHOST = ashost,
            SYSID = sysid,
            SYSNR = sysnr,
            CLIENT = client,
            LANG = lang,
            SAPROUTER = saprouter,
            GWHOST = gwhost,
            GWSERV = gwserv,
            PROGRAM_ID = programId,
        };

    private static Aff<RT, SncConfig> PromptSncSettings<RT>(SncConfig defaults)
        where RT : struct, HasAnsiConsole<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[bold]SNC Settings[/]")
        from myName in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("SNC_MYNAME (own SNC name):")
                .DefaultValue(defaults.SNC_MYNAME))
        from qop in AnsiConsole<RT>.prompt(
            new SelectionPrompt<string>()
                .Title("SNC_QOP (quality of protection):")
                .AddChoices(
                    "1 - Authentication only",
                    "2 - Integrity protection",
                    "3 - Privacy protection (encryption)",
                    "8 - Default",
                    "9 - Maximum"))
        let qopValue = qop[..1]
        select defaults with
        {
            SNC_MYNAME = myName,
            SNC_QOP = qopValue,
        };

    private static Aff<RT, Unit> AutoPseSetup<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>, HasProcessRunner<RT>,
            HasConfigService<RT>, HasPseService<RT> =>
        from _ in AnsiConsole<RT>.markupLine("\n[bold]PSE Setup[/]")
        from _1 in (
            from _a in PseService<RT>.ensurePseExists(config)
            from defaultCertPath in PseService<RT>.getDefaultCertPath(config.Pse.Path)
            from certPath in PseService<RT>.exportOwnCert(
                config.Pse.Path,
                defaultCertPath)
            from _b in AnsiConsole<RT>.markupLine(
                $"\n[yellow]Next step:[/] Import [bold]{Markup.Escape(certPath)}[/] " +
                "into SAP STRUST under the SNC identity.")
            from _c in AnsiConsole<RT>.markupLine(
                "[dim]To fetch and import the SAP server certificate automatically, " +
                "use 'sncverify check' after completing STRUST import.[/]")
            select unit
        ) | @catch(e => ManualPseFallback<RT>(e, config))
        select unit;

    private static Aff<RT, Unit> ManualPseFallback<RT>(Error error, SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>, HasConfigService<RT>, HasPseService<RT> =>
        from _ in AnsiConsole<RT>.markupLine(
            $"\n[yellow]PSE auto-setup failed:[/] {Markup.Escape(error.Message)}")
        from _1 in AnsiConsole<RT>.markupLine(
            "\n[bold]Manual SNC partner configuration[/]")
        from partnerName in AnsiConsole<RT>.prompt(
            new TextPrompt<string>("SNC_PARTNERNAME (SAP server SNC name, e.g. p:CN=SAPSERVER):")
                .DefaultValueIfNotEmpty(config.Snc.SNC_PARTNERNAME))
        // Save partner name to config
        let updatedConfig = config with { Snc = config.Snc with { SNC_PARTNERNAME = partnerName } }
        from configPath in ConfigService<RT>.getConfigPath()
        from _2 in ConfigService<RT>.writeConfig(configPath, updatedConfig)
        from _3 in AnsiConsole<RT>.markupLine(
            "[yellow]Complete certificate exchange manually:[/]\n" +
            "  1. sncverify own_cert export\n" +
            "  2. Import the exported certificate into SAP STRUST\n" +
            "  3. Export SAP's certificate from STRUST\n" +
            "  4. sncverify sap_cert import <sap-cert-file>")
        select unit;

    private static Aff<RT, Unit> PromptPartnerNameIfMissing<RT>(SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>, HasConfigService<RT> =>
        !string.IsNullOrEmpty(config.Snc.SNC_PARTNERNAME)
            ? AnsiConsole<RT>.markupLine(
                  $"[green]SNC_PARTNERNAME already configured:[/] " +
                  Markup.Escape(config.Snc.SNC_PARTNERNAME))
            : from _ in AnsiConsole<RT>.markupLine(
                  "\n[bold]Manual SNC partner configuration[/]")
              from partnerName in AnsiConsole<RT>.prompt(
                  new TextPrompt<string>("SNC_PARTNERNAME (SAP server SNC name, e.g. p:CN=SAPSERVER):"))
              let updatedConfig = config with { Snc = config.Snc with { SNC_PARTNERNAME = partnerName } }
              from configPath in ConfigService<RT>.getConfigPath()
              from _2 in ConfigService<RT>.writeConfig(configPath, updatedConfig)
              select unit;

    private static Eff<RT, Unit> ShowNextSteps<RT>(bool needsServer, SncVerifyConfig config)
        where RT : struct, HasAnsiConsole<RT>
    {
        var sncName = Markup.Escape(config.Snc.SNC_MYNAME);

        return
            from _ in AnsiConsole<RT>.markupLine("\n[bold]Next Steps[/]")
            from _client in AnsiConsole<RT>.markupLine(
                $"\n[bold]Client mode:[/]\n" +
                $"  In SAP, assign the SNC name [blue]{sncName}[/] to the SAP user\n" +
                "  (transaction SU01, SNC tab).\n" +
                "  Then verify with: [bold]sncverify run client[/]")
            from _server in needsServer
                ? AnsiConsole<RT>.markupLine(
                    $"\n[bold]Server mode:[/]\n" +
                    $"  In SAP, create an RFC destination (SM59) for program ID " +
                    $"[blue]{Markup.Escape(config.Connection.PROGRAM_ID)}[/]\n" +
                    $"  and assign the SNC name [blue]{sncName}[/] to the RFC user and to the RFC destination.\n" +
                    $"  Configure gateway security (SMGW reginfo) to allow the program ID.\n" +
                    "  Then verify with: [bold]sncverify run server[/]")
                : SuccessEff(unit)
            select unit;
    }
}

