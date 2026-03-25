using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;

namespace SncVerify.Sys;

public interface PseServiceIO
{
    string SapGenPseExecutable { get; }
    string LibraryDirectory { get; }
    string RfcLibraryName { get; }
    string CryptoLibraryName { get; }
    bool FileExists(string path);
    void EnsureDirectoryExists(string filePath);
    string GetDefaultCertPath(string psePath);
    string GeneratePin();
    string GetKeyFilePath(string psePath);
    void WriteKeyFile(string keyFilePath, string pin);
    string ReadKeyFile(string keyFilePath);
    bool KeyFileExists(string psePath);
}

public readonly struct LivePseServiceIO : PseServiceIO
{
    public static readonly PseServiceIO Default = new LivePseServiceIO();

    public string SapGenPseExecutable =>
        Path.Combine(LibraryDirectory, RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sapgenpse.exe"
            : "sapgenpse");

    public string LibraryDirectory => AppDomain.CurrentDomain.BaseDirectory;

    public string RfcLibraryName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sapnwrfc.dll"
            : "libsapnwrfc.so";

    public string CryptoLibraryName =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "sapcrypto.dll"
            : "libsapcrypto.so";

    public bool FileExists(string path) => File.Exists(path);

    public void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    public string GetDefaultCertPath(string psePath) =>
        Path.Combine(
            Path.GetDirectoryName(psePath)
                ?? Config.PseConfig.GetDefaultPseDir(),
            "own_cert.crt");

    public string GeneratePin()
    {
        const string upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string lower = "abcdefghijklmnopqrstuvwxyz";
        const string digits = "0123456789";
        const string special = "!@#$%^&*()-_=+";
        const string all = upper + lower + digits + special;

        Span<char> pin = stackalloc char[32];

        // ensure at least one of each category
        pin[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        pin[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        pin[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        pin[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

        for (var i = 4; i < pin.Length; i++)
            pin[i] = all[RandomNumberGenerator.GetInt32(all.Length)];

        // shuffle
        for (var i = pin.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (pin[i], pin[j]) = (pin[j], pin[i]);
        }

        return new string(pin);
    }

    public string GetKeyFilePath(string psePath) =>
        Path.ChangeExtension(psePath, ".key");

    public void WriteKeyFile(string keyFilePath, string pin)
    {
        EnsureDirectoryExists(keyFilePath);
        File.WriteAllText(keyFilePath, pin);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            ProtectFileForCurrentUserWindows(keyFilePath);
        else
            File.SetUnixFileMode(keyFilePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    public string ReadKeyFile(string keyFilePath) =>
        File.ReadAllText(keyFilePath).Trim();

    public bool KeyFileExists(string psePath) =>
        FileExists(GetKeyFilePath(psePath));

    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    private static void ProtectFileForCurrentUserWindows(string filePath)
    {
        var fileInfo = new FileInfo(filePath);
        var security = fileInfo.GetAccessControl();

        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);

        var currentUser = WindowsIdentity.GetCurrent().User!;
        security.SetOwner(currentUser);

        // remove all existing rules
        foreach (FileSystemAccessRule rule in security.GetAccessRules(true, true, typeof(SecurityIdentifier)))
            security.RemoveAccessRule(rule);

        // grant full control to current user only
        security.AddAccessRule(new FileSystemAccessRule(
            currentUser,
            FileSystemRights.FullControl,
            AccessControlType.Allow));

        fileInfo.SetAccessControl(security);
    }
}
