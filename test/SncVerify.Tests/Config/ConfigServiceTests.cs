using SncVerify.Config;
using SncVerify.Sys;
using SncVerify.Tests.Sys;

namespace SncVerify.Tests.Config;

/// <summary>
/// Tests for pure config operations (GetValue, SetValue, GetAllKeys).
/// </summary>
public class ConfigServiceTests
{
    [Theory]
    [InlineData("ASHOST", "sapserver.com")]
    [InlineData("SNC_MYNAME", "p:CN=TEST")]
    [InlineData("PROGRAM_ID", "SNCVERIFY")]
    [InlineData("PCS", "2")]
    [InlineData("SAPROUTER", "/H/router/H/")]
    public void GetValue_ValidKey_ReturnsValue(string key, string expected)
    {
        var config = new SncVerifyConfig
        {
            Connection = new ConnectionConfig { ASHOST = "sapserver.com", SAPROUTER = "/H/router/H/" },
            Snc = new SncConfig { SNC_MYNAME = "p:CN=TEST" },
        };

        var result = ConfigService.GetValue(config, key);
        Assert.True(result.IsRight);
        result.IfRight(v => Assert.Equal(expected, v));
    }

    [Fact]
    public void GetValue_CaseInsensitive()
    {
        var config = new SncVerifyConfig
        {
            Connection = new ConnectionConfig { ASHOST = "myhost" },
        };

        var result = ConfigService.GetValue(config, "ashost");
        Assert.True(result.IsRight);
        result.IfRight(v => Assert.Equal("myhost", v));
    }

    [Fact]
    public void GetValue_UnknownKey_ReturnsError()
    {
        var config = new SncVerifyConfig();
        var result = ConfigService.GetValue(config, "INVALID_KEY");
        Assert.True(result.IsLeft);
    }

    [Fact]
    public void SetValue_ValidKey_ReturnsUpdatedConfig()
    {
        var config = new SncVerifyConfig();
        var result = ConfigService.SetValue(config, "ASHOST", "newhost.com");

        Assert.True(result.IsRight);
        result.IfRight(c => Assert.Equal("newhost.com", c.Connection.ASHOST));
    }

    [Fact]
    public void SetValue_UnknownKey_ReturnsError()
    {
        var config = new SncVerifyConfig();
        var result = ConfigService.SetValue(config, "INVALID_KEY", "value");
        Assert.True(result.IsLeft);
    }

    [Fact]
    public void SetValue_DoesNotMutateOriginal()
    {
        var config = new SncVerifyConfig
        {
            Connection = new ConnectionConfig { ASHOST = "original.com" },
        };

        var result = ConfigService.SetValue(config, "ASHOST", "changed.com");

        Assert.Equal("original.com", config.Connection.ASHOST);
        result.IfRight(c => Assert.Equal("changed.com", c.Connection.ASHOST));
    }

    [Fact]
    public void GetAllKeys_ReturnsAllExpectedKeys()
    {
        var keys = ConfigService.GetAllKeys();
        Assert.Contains("ASHOST", keys);
        Assert.Contains("SNC_MYNAME", keys);
        Assert.Contains("PCS", keys);
        Assert.Contains("SAPROUTER", keys);
        Assert.Equal(14, keys.Count);
    }
}

/// <summary>
/// Tests for live ConfigServiceIO (file-based round-trip).
/// </summary>
public class LiveConfigServiceIOTests
{
    [Fact]
    public void ReadConfig_MissingFile_ReturnsDefaultConfig()
    {
        var io = new LiveConfigServiceIO();
        var result = io.ReadConfig("nonexistent_path.json");

        Assert.True(result.IsRight);
        result.IfRight(config =>
        {
            Assert.Equal("", config.Connection.ASHOST);
            Assert.Equal("00", config.Connection.SYSNR);
            Assert.NotEmpty(config.Pse.Path);
        });
    }

    [Fact]
    public void WriteAndReadConfig_RoundTrip_PreservesValues()
    {
        var io = new LiveConfigServiceIO();
        var tempFile = Path.Combine(Path.GetTempPath(), $"sncverify_test_{Guid.NewGuid()}.json");
        try
        {
            var config = new SncVerifyConfig
            {
                Connection = new ConnectionConfig
                {
                    ASHOST = "sapserver.test.com",
                    SYSID = "QFS",
                    SYSNR = "01",
                    CLIENT = "200",
                    LANG = "DE",
                    SAPROUTER = "/H/router/S/3299/H/",
                    GWHOST = "sapgw.test.com",
                    GWSERV = "sapgw01",
                    PROGRAM_ID = "TESTPROG",
                    REG_COUNT = "2",
                },
                Snc = new SncConfig
                {
                    SNC_QOP = "3",
                    SNC_MYNAME = "p:CN=TESTCLIENT",
                    SNC_PARTNERNAME = "p:CN=SAPSERVER",
                    SNC_SSO = "0",
                    PCS = "1",
                },
                Pse = new PseConfig
                {
                    Path = "/home/test/.sncverify/sec/SAPSNCS.pse",
                    SecuDir = "/home/test/.sncverify/sec",
                },
            };

            var writeResult = io.WriteConfig(tempFile, config);
            Assert.True(writeResult.IsRight);

            var readResult = io.ReadConfig(tempFile);
            Assert.True(readResult.IsRight);

            readResult.IfRight(loaded =>
            {
                Assert.Equal("sapserver.test.com", loaded.Connection.ASHOST);
                Assert.Equal("QFS", loaded.Connection.SYSID);
                Assert.Equal("01", loaded.Connection.SYSNR);
                Assert.Equal("200", loaded.Connection.CLIENT);
                Assert.Equal("DE", loaded.Connection.LANG);
                Assert.Equal("/H/router/S/3299/H/", loaded.Connection.SAPROUTER);
                Assert.Equal("sapgw.test.com", loaded.Connection.GWHOST);
                Assert.Equal("sapgw01", loaded.Connection.GWSERV);
                Assert.Equal("TESTPROG", loaded.Connection.PROGRAM_ID);
                Assert.Equal("2", loaded.Connection.REG_COUNT);

                Assert.Equal("3", loaded.Snc.SNC_QOP);
                Assert.Equal("p:CN=TESTCLIENT", loaded.Snc.SNC_MYNAME);
                Assert.Equal("p:CN=SAPSERVER", loaded.Snc.SNC_PARTNERNAME);
                Assert.Equal("0", loaded.Snc.SNC_SSO);
                Assert.Equal("1", loaded.Snc.PCS);

                Assert.Equal("/home/test/.sncverify/sec/SAPSNCS.pse", loaded.Pse.Path);
                Assert.Equal("/home/test/.sncverify/sec", loaded.Pse.SecuDir);
            });
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    [Fact]
    public void ReadConfig_InvalidJson_ReturnsError()
    {
        var io = new LiveConfigServiceIO();
        var tempFile = Path.Combine(Path.GetTempPath(), $"sncverify_test_{Guid.NewGuid()}.json");
        try
        {
            File.WriteAllText(tempFile, "not valid json{{{");
            var result = io.ReadConfig(tempFile);
            Assert.True(result.IsLeft);
        }
        finally
        {
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }
}

/// <summary>
/// Tests for trait-based ConfigService&lt;RT&gt; using TestRuntime.
/// </summary>
public class ConfigServiceTraitTests
{
    [Fact]
    public async Task ReadConfig_ViaTestRuntime_ReturnsStoredConfig()
    {
        var configIO = new TestConfigServiceIO();
        var expected = new SncVerifyConfig
        {
            Connection = new ConnectionConfig { ASHOST = "stored.host" },
        };
        configIO.SetConfig(configIO.GetConfigPath(), expected);

        var runtime = TestRuntime.New(configService: configIO);

        var effect =
            from path in ConfigService<TestRuntime>.getConfigPath()
            from config in ConfigService<TestRuntime>.readConfig(path)
            select config;

        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsSucc);
        result.IfSucc(c => Assert.Equal("stored.host", c.Connection.ASHOST));
    }

    [Fact]
    public async Task WriteConfig_ViaTestRuntime_StoresConfig()
    {
        var configIO = new TestConfigServiceIO();
        var runtime = TestRuntime.New(configService: configIO);

        var config = new SncVerifyConfig
        {
            Connection = new ConnectionConfig { ASHOST = "new.host" },
        };

        var effect =
            from path in ConfigService<TestRuntime>.getConfigPath()
            from _ in ConfigService<TestRuntime>.writeConfig(path, config)
            select unit;

        var result = await effect.Run(runtime).AsTask();

        Assert.True(result.IsSucc);
        Assert.NotNull(configIO.LastWrittenConfig);
        Assert.Equal("new.host", configIO.LastWrittenConfig!.Connection.ASHOST);
    }
}
