using System.Security.Cryptography;

namespace VaettirNet.VelopackExtensions.SignedReleases.Signing;

public record EcdsaSignatureOptions(DSASignatureFormat SignatureFormat)
{
    public static readonly EcdsaSignatureOptions Default = new(DSASignatureFormat.Rfc3279DerSequence);
}