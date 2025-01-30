using System.Security.Cryptography.X509Certificates;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class DefaultTrustResolver : IAssetTrustResolver
{
    public static readonly DefaultTrustResolver Instance = new();
    
    public AssetValidationResult Validate(VelopackAsset asset, X509Certificate2 signer)
    {
        return new UntrustedValidationResult(signer);
    }
}