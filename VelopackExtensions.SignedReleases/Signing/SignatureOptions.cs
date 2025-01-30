namespace VaettirNet.VelopackExtensions.SignedReleases.Signing;

public record struct SignatureOptions(RsaSignatureOptions Rsa = null, EcdsaSignatureOptions EcDsa = null);