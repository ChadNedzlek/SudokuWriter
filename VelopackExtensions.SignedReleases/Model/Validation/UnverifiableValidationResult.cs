namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record UnverifiableValidationResult(string CertHash, string Signature) : AssetValidationResult(ValidationResultCode.Unverifiable);