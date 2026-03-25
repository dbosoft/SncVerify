using LanguageExt;
using SncVerify.Config;
using SncVerify.Sys;
using SncVerify.Tests.Sys;
using Spectre.Console.Testing;

using static LanguageExt.Prelude;

namespace SncVerify.Tests.Pse;

public class PseServiceTests
{
    [Fact]
    public async Task CreatePse_Success_GeneratesPinAndWritesKeyFile()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueSuccess(); // gen_pse

        var pseService = new TestPseServiceIO();
        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var effect = PseService<TestRuntime>.createPse("/tmp/test.pse", "p:CN=TEST");
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        Assert.Single(processRunner.Calls);

        var (executable, arguments) = processRunner.Calls[0];
        Assert.Contains("sapgenpse", executable);
        Assert.Contains("gen_pse", arguments);
        Assert.Contains("-p \"/tmp/test.pse\"", arguments);
        // p: prefix stripped for sapgenpse DN
        Assert.Contains("\"CN=TEST\"", arguments);
        Assert.DoesNotContain("p:CN=TEST", arguments);
        // PIN was generated and passed to sapgenpse
        Assert.NotNull(pseService.LastGeneratedPin);
        Assert.Contains($"-x \"{pseService.LastGeneratedPin}\"", arguments);
        // key file was written
        Assert.Equal("/tmp/test.key", pseService.LastWrittenKeyFilePath);
    }

    [Fact]
    public async Task CreatePse_Failure_ReturnsFail()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueFailure(2, "PSE creation failed");

        var runtime = TestRuntime.New(processRunner: processRunner);

        var effect = PseService<TestRuntime>.createPse("/tmp/test.pse", "p:CN=TEST");
        var result = await effect.Run(runtime);

        Assert.True(result.IsFail);
    }

    [Fact]
    public async Task ExportOwnCert_Success_ReturnsCertPathAndPassesPin()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueSuccess();

        var pseService = new TestPseServiceIO();
        pseService.WriteKeyFile(pseService.GetKeyFilePath("/tmp/test.pse"), "mypin");

        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var effect = PseService<TestRuntime>.exportOwnCert("/tmp/test.pse", "/tmp/cert.crt");
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        result.IfSucc(path => Assert.Equal("/tmp/cert.crt", path));

        var (_, arguments) = processRunner.Calls[0];
        Assert.Contains("export_own_cert", arguments);
        Assert.Contains("-x \"mypin\"", arguments);
        Assert.Contains("-o \"/tmp/cert.crt\"", arguments);
    }

    [Fact]
    public async Task ImportCert_Success_PassesPinToSapgenpse()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueSuccess();

        var pseService = new TestPseServiceIO();
        pseService.WriteKeyFile(pseService.GetKeyFilePath("/tmp/test.pse"), "mypin");

        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var effect = PseService<TestRuntime>.importCert("/tmp/test.pse", "/tmp/remote.crt");
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);

        var (_, arguments) = processRunner.Calls[0];
        Assert.Contains("maintain_pk", arguments);
        Assert.Contains("-x \"mypin\"", arguments);
        Assert.Contains("-a \"/tmp/remote.crt\"", arguments);
    }

    [Fact]
    public async Task ListTrustedCerts_Success_ReturnsOutput()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueResult(new ProcessRunnerResult(0, "CN=SAP\nCN=TEST"));

        var pseService = new TestPseServiceIO();
        pseService.WriteKeyFile(pseService.GetKeyFilePath("/tmp/test.pse"), "mypin");

        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var effect = PseService<TestRuntime>.listTrustedCerts("/tmp/test.pse");
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        result.IfSucc(output => Assert.Contains("CN=SAP", output));
    }

    [Fact]
    public async Task EnsurePseExists_PseNotFound_CreatesPseWithKeyFile()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueSuccess(); // gen_pse
        processRunner.EnqueueSuccess(); // seclogin

        var pseService = new TestPseServiceIO();
        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var config = new SncVerifyConfig
        {
            Snc = new SncConfig { SNC_MYNAME = "p:CN=TEST" },
            Pse = new PseConfig { Path = "/tmp/test.pse" },
        };

        var effect = PseService<TestRuntime>.ensurePseExists(config);
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        Assert.Equal(2, processRunner.Calls.Count); // gen_pse + seclogin
        Assert.NotNull(pseService.LastGeneratedPin);
        Assert.Equal("/tmp/test.key", pseService.LastWrittenKeyFilePath);
    }

    [Fact]
    public async Task EnsurePseExists_PseExists_SkipsCreationButRunsSeclogin()
    {
        var processRunner = new TestProcessRunnerIO();
        processRunner.EnqueueSuccess(); // seclogin

        var pseService = new TestPseServiceIO();
        pseService.AddExistingFile("/tmp/test.pse");
        pseService.WriteKeyFile(pseService.GetKeyFilePath("/tmp/test.pse"), "existingpin");

        var runtime = TestRuntime.New(processRunner: processRunner, pseService: pseService);

        var config = new SncVerifyConfig
        {
            Snc = new SncConfig { SNC_MYNAME = "p:CN=TEST" },
            Pse = new PseConfig { Path = "/tmp/test.pse" },
        };

        var effect = PseService<TestRuntime>.ensurePseExists(config);
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        Assert.Single(processRunner.Calls); // only seclogin, no gen_pse
        var (_, args) = processRunner.Calls[0];
        Assert.Contains("seclogin", args);
    }

    [Fact]
    public async Task ReadPin_KeyFileExists_ReturnsPin()
    {
        var pseService = new TestPseServiceIO();
        pseService.WriteKeyFile("/tmp/test.key", "StoredPin123!");

        var runtime = TestRuntime.New(pseService: pseService);

        var effect = PseService<TestRuntime>.readPin("/tmp/test.pse");
        var result = await effect.Run(runtime);

        Assert.True(result.IsSucc);
        result.IfSucc(pin => Assert.Equal("StoredPin123!", pin));
    }

    [Fact]
    public async Task ReadPin_KeyFileMissing_ReturnsFail()
    {
        var pseService = new TestPseServiceIO();
        var runtime = TestRuntime.New(pseService: pseService);

        var effect = PseService<TestRuntime>.readPin("/tmp/test.pse");
        var result = await effect.Run(runtime);

        Assert.True(result.IsFail);
    }

    [Fact]
    public async Task GetDefaultCertPath_ReturnsCrtInSameDirectory()
    {
        var runtime = TestRuntime.New();
        var psePath = Path.Combine("some", "dir", "SAPSNCS.pse");

        var effect = PseService<TestRuntime>.getDefaultCertPath(psePath);
        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsSucc);
        result.IfSucc(path =>
            Assert.Equal(Path.Combine("some", "dir", "own_cert.crt"), path));
    }

    [Fact]
    public async Task CheckSapLibraries_AllPresent_Succeeds()
    {
        var pseService = new TestPseServiceIO();
        pseService.AddSapLibraries();

        var runtime = TestRuntime.New(pseService: pseService);

        var effect = PseService<TestRuntime>.checkSapLibraries();
        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsSucc);
    }

    [Fact]
    public async Task CheckSapLibraries_MissingRfcLib_Fails()
    {
        var pseService = new TestPseServiceIO();
        // only add crypto + sapgenpse, not RFC lib
        pseService.AddExistingFile(Path.Combine(pseService.LibraryDirectory, pseService.CryptoLibraryName));
        pseService.AddExistingFile(pseService.SapGenPseExecutable);

        var runtime = TestRuntime.New(pseService: pseService);

        var effect = PseService<TestRuntime>.checkSapLibraries();
        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsFail);
    }

    [Fact]
    public async Task CheckSapLibraries_MissingSapgenpse_Fails()
    {
        var pseService = new TestPseServiceIO();
        pseService.AddExistingFile(Path.Combine(pseService.LibraryDirectory, pseService.RfcLibraryName));
        pseService.AddExistingFile(Path.Combine(pseService.LibraryDirectory, pseService.CryptoLibraryName));
        // sapgenpse missing

        var runtime = TestRuntime.New(pseService: pseService);

        var effect = PseService<TestRuntime>.checkSapLibraries();
        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsFail);
    }
}
