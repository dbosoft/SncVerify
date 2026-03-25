using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using LanguageExt.Effects.Traits;
using SncVerify.Sys;
using Spectre.Console;

namespace SncVerify.Sap;

/// <summary>
/// SNC client connection test logic. Reusable from run client command and check command.
/// </summary>
public static class SapClientLogic
{
    public record ClientTestResult(
        bool PingSuccessful,
        string User,
        string UserFullName);

    public static Aff<RT, ClientTestResult> run<RT>(IConnection connection)
        where RT : struct, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT> =>
        from _ in AnsiConsole<RT>.markupLine("[dim]Testing SNC connection...[/]")
        from _ping in SapRfcCalls<RT>.ping(connection)
        from __ in AnsiConsole<RT>.markupLine("[green]SNC connection successful.[/]")
        from attrs in SapRfcCalls<RT>.getConnectionAttributes(connection)
        from _1 in AnsiConsole<RT>.markupLine($"[dim]Connected as user:[/] {Markup.Escape(attrs.User)}")
        from fullName in SapRfcCalls<RT>.getUserFullName(connection, attrs.User)
        from _2 in AnsiConsole<RT>.markupLine(
            $"[green]Data exchange verified.[/] User: {Markup.Escape(fullName)}")
        select new ClientTestResult(
            PingSuccessful: true,
            User: attrs.User,
            UserFullName: fullName);

    public static Eff<RT, Unit> renderResult<RT>(ClientTestResult result)
        where RT : struct, HasAnsiConsole<RT>
    {
        var table = new Table()
            .AddColumn("Check")
            .AddColumn("Result");

        table.AddRow("SNC RFC ping", result.PingSuccessful ? "[green]OK[/]" : "[red]Failed[/]");
        table.AddRow("User", Markup.Escape(result.User));
        table.AddRow("Full name", Markup.Escape(result.UserFullName));

        return
            from _ in AnsiConsole<RT>.markupLine("\n[bold]Client Test Summary[/]")
            from __ in AnsiConsole<RT>.write(table)
            select unit;
    }
}
