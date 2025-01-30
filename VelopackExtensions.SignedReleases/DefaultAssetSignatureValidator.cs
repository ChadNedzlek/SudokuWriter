using System;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class DefaultAssetSignatureValidator : IAssetSignatureValidator
{
    public static readonly DefaultAssetSignatureValidator Instance = new();
    
    public bool VerifyAssetHash(Span<byte> assetSha256Hash, Span<byte> signature, SigningProcessor certificate)
    {
        return certificate.VerifyHash(assetSha256Hash, signature);
    }
}