using LanguageExt.Effects.Traits;

namespace SncVerify.Sys;

public interface HasAnsiConsole<RT> : HasCancel<RT>
    where RT : struct, HasAnsiConsole<RT>, HasCancel<RT>
{
    Eff<RT, AnsiConsoleIO> AnsiConsoleEff { get; }
}
