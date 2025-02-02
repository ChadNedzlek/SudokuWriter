using System;
using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record CertificateVerificationFailedValidationResult(X509Certificate2 Signer, DateTimeOffset Whe, X509ChainStatusFlags StatusFlag) : AssetValidationResult(ValidationResultCode.CertificateVerificationFailed);