using LanguageExt.Effects.Traits;

namespace SncVerify.Sys;

public interface HasPseService<RT> : HasCancel<RT>
    where RT : struct, HasPseService<RT>, HasCancel<RT>
{
    Eff<RT, PseServiceIO> PseServiceEff { get; }
}
