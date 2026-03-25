using LanguageExt.Effects.Traits;

namespace SncVerify.Sys;

public interface HasProcessRunner<RT> : HasCancel<RT>
    where RT : struct, HasProcessRunner<RT>, HasCancel<RT>
{
    Eff<RT, ProcessRunnerIO> ProcessRunnerEff { get; }
}
