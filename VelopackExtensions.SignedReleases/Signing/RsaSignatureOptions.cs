using System.Security.Cryptography;

namespace VaettirNet.VelopackExtensions.SignedReleases.Signing;

public record RsaSignatureOptions(HashAlgorithmName HashAlgorithmName, RSASignaturePadding Padding)
{
    public static readonly RsaSignatureOptions Default = new(HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
}