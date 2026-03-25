using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using SncVerify.Config;

namespace SncVerify;

[ExcludeFromCodeCoverage]
public static class RfcLibraryHelper
{
    /// <summary>
    /// Ensures PATH/LD_LIBRARY_PATH includes the executable directory (for sapnwrfc)
    /// and sets SECUDIR, SNC_LIB, SNC_LIB_64.
    /// </summary>
    public static void EnsureEnvironment()
    {
        EnsureLibraryPath();

        Environment.SetEnvironmentVariable("SECUDIR", PseConfig.GetDefaultPseDir());

        var sncLibName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sapcrypto.dll"
            : "libsapcrypto.so";

        var sncLib = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sncLibName);

        Environment.SetEnvironmentVariable("SNC_LIB", sncLib);
        Environment.SetEnvironmentVariable("SNC_LIB_64", sncLib);
    }

    private static void EnsureLibraryPath()
    {
        var executableDir = AppDomain.CurrentDomain.BaseDirectory;
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var pathVariableName = isWindows ? "PATH" : "LD_LIBRARY_PATH";
        var separator = isWindows ? ';' : ':';

        var currentPath = Environment.GetEnvironmentVariable(pathVariableName) ?? "";

        if (currentPath.Contains(executableDir))
            return;

        var newPath = string.IsNullOrEmpty(currentPath)
            ? executableDir
            : $"{currentPath}{separator}{executableDir}";

        Environment.SetEnvironmentVariable(pathVariableName, newPath);
    }
}
