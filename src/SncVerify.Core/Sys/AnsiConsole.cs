using Spectre.Console;
using Spectre.Console.Rendering;

namespace SncVerify.Sys;

public static class AnsiConsole<RT> where RT : struct, HasAnsiConsole<RT>
{
    public static Eff<RT, Unit> markupLine(string text) =>
        default(RT).AnsiConsoleEff.Map(fun((AnsiConsoleIO io) => io.AnsiConsole.MarkupLine(text)));

    public static Eff<RT, Unit> writeLine(string text) =>
        default(RT).AnsiConsoleEff.Map(fun((AnsiConsoleIO io) => io.AnsiConsole.WriteLine(text)));

    public static Eff<RT, Unit> write(IRenderable renderable) =>
        default(RT).AnsiConsoleEff.Map(fun((AnsiConsoleIO io) => io.AnsiConsole.Write(renderable)));

    public static Aff<RT, bool> confirm(string prompt, bool defaultValue) =>
        from cancelToken in cancelToken<RT>()
        let consolePrompt = new ConfirmationPrompt(prompt)
        {
            DefaultValue = defaultValue,
        }
        from result in default(RT).AnsiConsoleEff.MapAsync(
            async ac => await consolePrompt.ShowAsync(ac.AnsiConsole, cancelToken))
        select result;

    public static Aff<RT, T> prompt<T>(IPrompt<T> prompt) =>
        from cancelToken in cancelToken<RT>()
        from result in default(RT).AnsiConsoleEff.MapAsync(
            async ac => await prompt.ShowAsync(ac.AnsiConsole, cancelToken))
        select result;

    public static Aff<RT, T> withSpinner<T>(string text, Aff<RT, T> aff) =>
        from ansiConsole in default(RT).AnsiConsoleEff
        from result in AffMaybe<RT, T>(async rt =>
        {
            return await ansiConsole.AnsiConsole.Status().StartAsync(
                text,
                async _ => await aff.Run(rt));
        })
        select result;
}
