using SncVerify.Sys;

namespace SncVerify.Commands;

public static class RunHelper
{
    public static Task<int> Run<RT>(Aff<RT, Unit> action, RT runtime)
        where RT : struct, HasAnsiConsole<RT> =>
        action
            .Run(runtime)
            .AsTask()
            .ContinueWith(t => t.Result.Match(
                Succ: _ => 0,
                Fail: error =>
                {
                    WriteErrorToConsole(error);
                    return error.Code != 0 ? error.Code : 1;
                }));

    private static void WriteErrorToConsole(Error error)
    {
        Spectre.Console.AnsiConsole.MarkupLine(
            $"[red]Error: {Spectre.Console.Markup.Escape(error.Message)}[/]");

        error.Inner.IfSome(WriteErrorToConsole);

        error.Exception.IfSome(ex =>
        {
            for (var inner = ex.InnerException; inner != null; inner = inner.InnerException)
                Spectre.Console.AnsiConsole.MarkupLine(
                    $"[red dim]  {Spectre.Console.Markup.Escape(inner.Message)}[/]");
        });
    }
}
