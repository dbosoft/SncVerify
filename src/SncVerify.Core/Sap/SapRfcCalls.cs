using Dbosoft.YaNco;
using Dbosoft.YaNco.Traits;
using LanguageExt.Effects.Traits;

namespace SncVerify.Sap;

/// <summary>
/// Pure RFC function module wrappers. No UI, no side effects beyond the RFC call.
/// </summary>
public static class SapRfcCalls<RT>
    where RT : struct, HasSAPRfc<RT>, HasCancel<RT>
{
    public static Aff<RT, Unit> ping(IConnection connection) =>
        SAPRfc<RT>.ping(connection);

    public static Aff<RT, string> readProfileParameter(
        IConnection connection, string paramName) =>
        SAPRfc<RT>.callFunction(connection, "TH_GET_PARAMETER",
            Input: f => f.SetField("PARAMETER_NAME", paramName),
            Output: f => f.GetField<string>("PARAMETER_VALUE"));

    public static Aff<RT, byte[]> getOwnCertificate(IConnection connection) =>
        SAPRfc<RT>.callFunction(connection, "SSFR_GET_OWNCERTIFICATE",
            Input: f => f
                .SetStructure("IS_STRUST_IDENTITY", s => s
                    .SetField("PSE_CONTEXT", "PROG")
                    .SetField("PSE_APPLIC", "<SNCS>")),
            Output: f => f.GetField<byte[]>("EV_CERTIFICATE"));

    public static Aff<RT, Seq<byte[]>> getCertificateList(IConnection connection) =>
        SAPRfc<RT>.callFunction(connection, "SSFR_GET_CERTIFICATELIST",
            Input: f => f
                .SetStructure("IS_STRUST_IDENTITY", s => s
                    .SetField("PSE_CONTEXT", "PROG")
                    .SetField("PSE_APPLIC", "<SNCS>")),
            Output: f => f
                .MapTable("ET_CERTIFICATELIST", row =>
                    row.GetFieldBytes(""))
                .Map(certs => certs.ToSeq()));

    public static Aff<RT, ConnectionAttributes> getConnectionAttributes(IConnection connection) =>
        from attrs in connection.GetAttributes().ToAff(l => l)
        select attrs;

    public static Aff<RT, string> getUserFullName(IConnection connection, string userName) =>
        SAPRfc<RT>.callFunction(connection, "BAPI_USER_GET_DETAIL",
            Input: f => f.SetField("USERNAME", userName),
            Output: f => f
                .MapStructure("ADDRESS", s => s.GetField<string>("FULLNAME")));
}
