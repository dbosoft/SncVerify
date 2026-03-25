using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using LanguageExt;
using LanguageExt.Effects.Traits;
using SncVerify.Sys;

namespace SncVerify;

public static class SAPIDocServer<RT> where RT :
    struct, HasSAPRfcServer<RT>, HasSAPRfc<RT>, HasCancel<RT>, HasAnsiConsole<RT>
{
    public static Aff<RT, Unit> processInboundIDoc(CalledFunction<RT> cf) => cf
        .Input(i => i)
        .Process(_ => AnsiConsole<RT>.markupLine(
            $"[green]IDoc received[/] at {DateTime.Now:HH:mm:ss}"))
        .NoReply();
}
