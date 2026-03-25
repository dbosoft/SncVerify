using LanguageExt.Effects.Traits;

namespace SncVerify.Sys;

public interface HasConfigService<RT> : HasCancel<RT>
    where RT : struct, HasConfigService<RT>, HasCancel<RT>
{
    Eff<RT, ConfigServiceIO> ConfigServiceEff { get; }
}
