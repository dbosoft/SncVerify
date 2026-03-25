using Spectre.Console;

namespace SncVerify.Commands;

internal static class TextPromptExtensions
{
    public static TextPrompt<string> DefaultValueIfNotEmpty(this TextPrompt<string> prompt, string value) =>
        string.IsNullOrEmpty(value) ? prompt : prompt.DefaultValue(value);
}
