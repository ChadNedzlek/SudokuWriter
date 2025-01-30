using System;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases.Services;

public interface IAssetSignatureValidator
{
    bool VerifyAssetHash(Span<byte> assetSha256Hash, Span<byte> signature, SigningProcessor certificate);
}