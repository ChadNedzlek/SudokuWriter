using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record UntrustedValidationResult(X509Certificate2 Signer) : AssetValidationResult(ValidationResultCode.Untrusted);