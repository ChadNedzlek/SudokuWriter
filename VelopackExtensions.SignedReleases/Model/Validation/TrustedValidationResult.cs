using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record TrustedValidationResult(X509Certificate2 Signer) : AssetValidationResult(ValidationResultCode.Trusted);