# SncVerify

A self-service SNC diagnostic and verification tool for SAP Basis administrators. Covers both **secure client connectivity** (RFC client with SNC) and **secure gateway** (RFC server registration with SNC) scenarios.

## Prerequisites

Place the following SAP libraries in the same directory as the `sncverify` executable:

| Library | Source |
|---------|--------|
| `sapnwrfc.dll` / `libsapnwrfc.so` | [SAP NW RFC SDK](https://support.sap.com/en/product/connectors/nwrfcsdk.html) |
| `sapcrypto.dll` / `libsapcrypto.so` | [SAPCRYPTOLIB](https://me.sap.com/softwarecenter) |
| `sapgenpse` / `sapgenpse.exe` | Included with SAPCRYPTOLIB |

Download the x64 files for your OS (Windows_X64 / Linux_X64)

## Quick Start

```bash
# 1. Run the setup wizard
sncverify setup

# 2. Check SNC configuration against SAP system
sncverify check

# 3. Test SNC client connection (SSO)
sncverify run client

# 4. Test SNC server registration (IDOC receiver)
sncverify run server
```

## Commands

### Setup

```bash
sncverify setup
```

Interactive wizard that:
- Prompts for SAP connection and SNC settings
- Creates a PSE with auto-generated PIN and SSO credentials
- Connects to SAP to read SNC parameters and exchange certificates
- Shows next steps for SAP-side configuration

### Check

```bash
sncverify check              # Full check (local + remote)
sncverify check --local-only # Local checks only
```

Verifies:
- SAP libraries present in application directory
- PSE file exists
- Configuration complete
- SAP SNC parameters (`snc/enable`, `snc/identity/as`, `snc/gssapi_lib`)
- SAP server certificate imported locally
- Own certificate trusted by SAP (STRUST)

### Run

```bash
sncverify run client  # Test SNC as RFC client (SSO)
sncverify run server  # Test SNC as RFC server (IDOC receiver)
```

**Client mode** connects to SAP with SNC enabled, performs an RFC ping, and calls `BAPI_USER_GET_DETAIL` to verify data exchange.

**Server mode** registers at the SAP gateway, listens for inbound IDOCs, and displays server state changes and errors.

### Configuration

```bash
sncverify config list           # Show current configuration
sncverify config set <key> <val> # Set a value
sncverify config get <key>       # Get a value
```

Configuration keys use SAP RFC SDK parameter names:
`ASHOST`, `SYSID`, `SYSNR`, `CLIENT`, `LANG`, `SAPROUTER`, `GWHOST`, `GWSERV`, `PROGRAM_ID`, `SNC_QOP`, `SNC_MYNAME`, `SNC_PARTNERNAME`, `SNC_SSO`, `PCS`

### Certificate Management

```bash
sncverify own_cert show           # Show own certificate details
sncverify own_cert export         # Export own certificate
sncverify sap_cert show           # Show trusted SAP certificates
sncverify sap_cert import <file>  # Import SAP certificate
```

## Configuration File

Settings are stored in `sncverify.json` in the application directory. Edit via `sncverify setup` or `sncverify config set`.

```json
{
  "connection": {
    "ASHOST": "sapserver.example.com",
    "SYSNR": "00",
    "CLIENT": "100",
    "LANG": "EN",
    "SAPROUTER": "",
    "GWHOST": "",
    "GWSERV": "",
    "PROGRAM_ID": "SNCVERIFY"
  },
  "snc": {
    "SNC_QOP": "3",
    "SNC_MYNAME": "p:CN=SNCVERIFY, O=dbosoft",
    "SNC_PARTNERNAME": "p:CN=SAPSERVER",
    "SNC_SSO": "1",
    "PCS": "2"
  }
}
```

## PSE Management

The PSE is auto-managed in the user profile directory:
- **Windows:** `%APPDATA%\sncverify\sec\SAPSNCS.pse`
- **Linux:** `~/.sncverify/sec/SAPSNCS.pse`

A complex PIN is auto-generated and stored in a key file with user-only permissions. SSO credentials are created automatically via `sapgenpse seclogin`.

## SAP-Side Configuration

### Client Mode

1. In SAP transaction **SU01**, assign the SNC name (e.g. `p:CN=SNCVERIFY, O=dbosoft`) to a SAP user on the SNC tab
2. In **STRUST**, import the certificate from `sncverify own_cert export` into the SNC (SAPSNCS) PSE

### Server Mode

1. In **SM59**, create an RFC destination for the program ID (e.g. `SNCVERIFY`)
2. Assign the SNC name to the RFC user and to the RFC destination and enable SNC in RFC
3. In **SMGW**, configure gateway security (reginfo) to allow the program ID
4. In **STRUST**, import the certificate as above

## Required SAP Authorizations

| Function Module | Purpose |
|----------------|---------|
| `TH_GET_PARAMETER` | Read SAP profile parameters |
| `SSFR_GET_OWNCERTIFICATE` | Read SAP's SNC certificate |
| `SSFR_GET_CERTIFICATELIST` | Read trusted certificate list |
| `BAPI_USER_GET_DETAIL` | Verify user details (client test) |

## Building

```bash
dotnet build SncVerify.slnx

# Publish single-file executable
dotnet publish src/SncVerify -r win-x64
dotnet publish src/SncVerify -r linux-x64
```

## License

See [LICENSE](LICENSE) for details.
