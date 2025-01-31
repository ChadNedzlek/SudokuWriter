using System.Text.Json.Serialization;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model;

public record SignedAsset : VelopackAsset
{
    [JsonConstructor]
    private SignedAsset()
    {
    }

    public SignedAsset(string signatureBase64, string certHash, VelopackAsset baseAsset)
    {
        SignatureBase64 = signatureBase64;
        CertHash = certHash;
        
        baseAsset.CopyTo(this);
    }

    public string SignatureBase64 { get; set; }
    public string CertHash { get; set; }
}