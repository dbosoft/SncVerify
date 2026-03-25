using SncVerify;
using SncVerify.Commands.Check;
using SncVerify.Commands.Config;
using SncVerify.Commands.Pse;
using SncVerify.Commands.Run;
using SncVerify.Commands.Setup;
using Spectre.Console.Cli;

// Set up environment for SAP RFC/SNC libraries (PATH, SECUDIR, SNC_LIB)
RfcLibraryHelper.EnsureEnvironment();

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("sncverify");
    config.SetApplicationVersion("2.0.0");

    config.AddCommand<SetupCommand>("setup")
        .WithDescription("Interactive guided setup wizard");

    config.AddCommand<CheckCommand>("check")
        .WithDescription("Run SNC diagnostic checks (local + remote)");

    config.AddBranch("config", cfg =>
    {
        cfg.SetDescription("Configuration management");

        cfg.AddCommand<ConfigListCommand>("list")
            .WithDescription("Show current configuration");

        cfg.AddCommand<ConfigSetCommand>("set")
            .WithDescription("Set a configuration value");

        cfg.AddCommand<ConfigGetCommand>("get")
            .WithDescription("Get a configuration value");
    });

    config.AddBranch("run", run =>
    {
        run.SetDescription("Run SNC verification scenarios");

        run.AddCommand<RunClientCommand>("client")
            .WithDescription("Test SNC as RFC client");

        run.AddCommand<RunServerCommand>("server")
            .WithDescription("Test SNC as RFC server (gateway registration)");
    });

    config.AddBranch("own_cert", own =>
    {
        own.SetDescription("Own certificate operations");

        own.AddCommand<OwnCertExportCommand>("export")
            .WithDescription("Export own certificate");

        own.AddCommand<OwnCertShowCommand>("show")
            .WithDescription("Show own certificate details");
    });

    config.AddBranch("sap_cert", sap =>
    {
        sap.SetDescription("SAP server certificate operations");

        sap.AddCommand<SapCertImportCommand>("import")
            .WithDescription("Import SAP certificate");

        sap.AddCommand<SapCertShowCommand>("show")
            .WithDescription("Show trusted SAP certificates");
    });
});

return await app.RunAsync(args);
