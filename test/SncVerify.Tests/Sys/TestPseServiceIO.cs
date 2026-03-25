using SncVerify.Sys;

namespace SncVerify.Tests.Sys;

public class TestPseServiceIO : PseServiceIO
{
    private readonly System.Collections.Generic.HashSet<string> _existingFiles = [];
    private readonly System.Collections.Generic.Dictionary<string, string> _keyFiles = new();

    public string SapGenPseExecutable => "sapgenpse";
    public string LibraryDirectory => "/app";
    public string RfcLibraryName => "sapnwrfc.dll";
    public string CryptoLibraryName => "sapcrypto.dll";

    public string? LastGeneratedPin { get; private set; }
    public string? LastWrittenKeyFilePath { get; private set; }

    public void AddExistingFile(string path) => _existingFiles.Add(path);

    public bool FileExists(string path) => _existingFiles.Contains(path);

    public void EnsureDirectoryExists(string filePath) { }

    public string GetDefaultCertPath(string psePath) =>
        Path.Combine(
            Path.GetDirectoryName(psePath) ?? "/tmp",
            "own_cert.crt");

    public string GeneratePin()
    {
        LastGeneratedPin = "TestPin123!Complex";
        return LastGeneratedPin;
    }

    public string GetKeyFilePath(string psePath) =>
        Path.ChangeExtension(psePath, ".key");

    public void WriteKeyFile(string keyFilePath, string pin)
    {
        LastWrittenKeyFilePath = keyFilePath;
        _keyFiles[keyFilePath] = pin;
    }

    public string ReadKeyFile(string keyFilePath) =>
        _keyFiles.TryGetValue(keyFilePath, out var pin)
            ? pin
            : throw new FileNotFoundException($"Key file not found: {keyFilePath}");

    public bool KeyFileExists(string psePath) =>
        _keyFiles.ContainsKey(GetKeyFilePath(psePath));

    /// <summary>
    /// Adds all three SAP library files so checkSapLibraries passes.
    /// </summary>
    public void AddSapLibraries()
    {
        AddExistingFile(Path.Combine(LibraryDirectory, RfcLibraryName));
        AddExistingFile(Path.Combine(LibraryDirectory, CryptoLibraryName));
        AddExistingFile(SapGenPseExecutable);
    }
}
