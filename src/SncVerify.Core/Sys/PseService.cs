using SncVerify.Config;

namespace SncVerify.Sys;

public static class PseService<RT>
    where RT : struct, HasPseService<RT>, HasProcessRunner<RT>, HasAnsiConsole<RT>
{
    public static Eff<RT, Unit> checkSapLibraries() =>
        from io in default(RT).PseServiceEff
        let rfcLib = Path.Combine(io.LibraryDirectory, io.RfcLibraryName)
        let cryptoLib = Path.Combine(io.LibraryDirectory, io.CryptoLibraryName)
        let genPse = io.SapGenPseExecutable
        from _ in !io.FileExists(rfcLib)
            ? FailEff<Unit>(Error.New(
                $"SAP RFC library not found: {rfcLib}\n" +
                "Download the SAP NW RFC SDK from the SAP Software Center and place it in the application directory."))
            : !io.FileExists(cryptoLib)
                ? FailEff<Unit>(Error.New(
                    $"SAP Crypto library not found: {cryptoLib}\n" +
                    "Download SAPCRYPTOLIB from the SAP Software Center and place it in the application directory."))
                : !io.FileExists(genPse)
                    ? FailEff<Unit>(Error.New(
                        $"sapgenpse not found: {genPse}\n" +
                        "sapgenpse is included with SAPCRYPTOLIB. Place it in the application directory."))
                    : SuccessEff(unit)
        from _1 in AnsiConsole<RT>.markupLine(
            $"[green]SAP libraries found in[/] {Spectre.Console.Markup.Escape(io.LibraryDirectory)}")
        select unit;

    public static Aff<RT, Unit> ensurePseExists(SncVerifyConfig config) =>
        from io in default(RT).PseServiceEff
        from _ in io.FileExists(config.Pse.Path)
            ? AnsiConsole<RT>.markupLine(
                $"[green]PSE found:[/] {Spectre.Console.Markup.Escape(config.Pse.Path)}")
            : from _1 in AnsiConsole<RT>.markupLine("[yellow]PSE not found, creating...[/]")
              from _2 in createPse(config.Pse.Path, config.Snc.SNC_MYNAME)
              select unit
        from _seclogin in createSecLogin(config.Pse.Path)
        select unit;

    public static Aff<RT, Unit> createPse(string psePath, string sncName) =>
        from io in default(RT).PseServiceEff
        from _0 in Eff(fun(() => { io.EnsureDirectoryExists(psePath); return unit; }))
        let pin = io.GeneratePin()
        let dn = SncNameToDn(sncName)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"gen_pse -v -p \"{psePath}\" -x \"{pin}\" \"{dn}\"")
        from _ in checkResult(result, "gen_pse")
        from _1 in Eff(fun(() => { io.WriteKeyFile(io.GetKeyFilePath(psePath), pin); return unit; }))
        from __ in AnsiConsole<RT>.markupLine("[green]PSE created successfully.[/]")
        from _2 in AnsiConsole<RT>.markupLine(
            $"[dim]PIN saved to {Spectre.Console.Markup.Escape(io.GetKeyFilePath(psePath))}[/]")
        select unit;

    public static Aff<RT, Unit> createSecLogin(string psePath) =>
        from io in default(RT).PseServiceEff
        from pin in readPin(psePath)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"seclogin -p \"{psePath}\" -x \"{pin}\"")
        from _ in checkResult(result, "seclogin")
        from __ in AnsiConsole<RT>.markupLine("[green]SSO credentials created for PSE.[/]")
        select unit;

    public static Aff<RT, string> readPin(string psePath) =>
        from io in default(RT).PseServiceEff
        let keyFilePath = io.GetKeyFilePath(psePath)
        from pin in io.KeyFileExists(psePath)
            ? Eff(fun(() => io.ReadKeyFile(keyFilePath)))
            : FailEff<string>(Error.New(
                $"Key file not found: {keyFilePath}. Run 'sncverify setup' to recreate the PSE."))
        select pin;

    public static Aff<RT, string> exportOwnCert(string psePath, string certPath) =>
        from io in default(RT).PseServiceEff
        from pin in readPin(psePath)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"export_own_cert -v -p \"{psePath}\" -x \"{pin}\" -o \"{certPath}\"")
        from _ in checkResult(result, "export_own_cert")
        from __ in AnsiConsole<RT>.markupLine(
            $"[green]Certificate exported to[/] {Spectre.Console.Markup.Escape(certPath)}")
        select certPath;

    public static Aff<RT, Unit> importCert(string psePath, string certPath) =>
        from io in default(RT).PseServiceEff
        from pin in readPin(psePath)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"maintain_pk -v -p \"{psePath}\" -x \"{pin}\" -a \"{certPath}\"")
        from _ in checkResult(result, "maintain_pk -a")
        from __ in AnsiConsole<RT>.markupLine("[green]Certificate imported successfully.[/]")
        select unit;

    public static Aff<RT, string> listTrustedCerts(string psePath) =>
        from io in default(RT).PseServiceEff
        from pin in readPin(psePath)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"maintain_pk -v -p \"{psePath}\" -x \"{pin}\" -l")
        from _ in checkResult(result, "maintain_pk -l")
        let output = string.Join(Environment.NewLine,
            new[] { result.Output, result.StdErr }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
        select output;

    public static Aff<RT, string> getMyName(string psePath) =>
        from io in default(RT).PseServiceEff
        from pin in readPin(psePath)
        from result in ProcessRunner<RT>.runProcess(
            io.SapGenPseExecutable,
            $"get_my_name -v -p \"{psePath}\" -x \"{pin}\"")
        from _ in checkResult(result, "get_my_name")
        let output = string.Join(Environment.NewLine,
            new[] { result.Output, result.StdErr }
                .Where(s => !string.IsNullOrWhiteSpace(s)))
        select output;

    public static Eff<RT, string> getDefaultCertPath(string psePath) =>
        default(RT).PseServiceEff.Map(io => io.GetDefaultCertPath(psePath));

    /// <summary>
    /// Converts an SNC name (e.g. "p:CN=SNCVERIFY, O=dbosoft") to a distinguished name
    /// for sapgenpse (e.g. "CN=SNCVERIFY, O=dbosoft") by stripping the "p:" prefix.
    /// </summary>
    internal static string SncNameToDn(string sncName) =>
        sncName.StartsWith("p:", StringComparison.OrdinalIgnoreCase)
            ? sncName[2..]
            : sncName;

    private static Aff<RT, Unit> checkResult(ProcessRunnerResult result, string command) =>
        result.ExitCode == 0
            ? SuccessAff(unit)
            : FailAff<Unit>(Error.New(
                $"sapgenpse {command} failed (exit {result.ExitCode}): {result.StdErr}"));
}
