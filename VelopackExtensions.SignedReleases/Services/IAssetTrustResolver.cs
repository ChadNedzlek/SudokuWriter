using System.Security.Cryptography.X509Certificates;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Services;

public interface IAssetTrustResolver
{
    AssetValidationResult Validate(VelopackAsset asset, X509Certificate2 signer);
}