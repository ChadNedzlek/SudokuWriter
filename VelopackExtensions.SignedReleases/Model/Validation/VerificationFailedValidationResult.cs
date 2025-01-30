using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record VerificationFailedValidationResult(X509Certificate2 Signer, string Signature) : AssetValidationResult(ValidationResultCode.VerificationFailed);