using System;
using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record ExpiredCertificateValidationResult(X509Certificate2 Signer, DateTimeOffset When) : AssetValidationResult(ValidationResultCode.Expired);