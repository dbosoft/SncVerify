using Spectre.Console;

namespace SncVerify.Sys;

public interface AnsiConsoleIO
{
    IAnsiConsole AnsiConsole { get; }
}

public readonly struct LiveAnsiConsoleIO(IAnsiConsole ansiConsole) : AnsiConsoleIO
{
    public static readonly AnsiConsoleIO Default = new LiveAnsiConsoleIO(Spectre.Console.AnsiConsole.Console);

    public IAnsiConsole AnsiConsole => ansiConsole;
}
