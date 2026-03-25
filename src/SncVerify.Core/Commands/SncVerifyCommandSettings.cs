using System.ComponentModel;
using Spectre.Console.Cli;

namespace SncVerify.Commands;

public class SncVerifyCommandSettings : CommandSettings
{
    [CommandOption("--batch|-b")]
    [Description("Non-interactive mode, no user prompts")]
    public bool BatchMode { get; set; }

    [CommandOption("--debug")]
    [Description("Enable debug output")]
    public bool DebugMode { get; set; }
}
