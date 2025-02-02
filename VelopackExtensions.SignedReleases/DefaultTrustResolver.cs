using System;
using System.Security.Cryptography.X509Certificates;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class DefaultTrustResolver : IAssetTrustResolver
{
    public static readonly DefaultTrustResolver Instance = new();

    public DefaultTrustResolver()
    {
    }

    public AssetValidationResult Validate(VelopackAsset asset, X509Certificate2 signer, DateTimeOffset asOf)
    {
        X509ChainStatusFlags status = signer.VerifyChain(
            new X509ChainPolicy
            {
                RevocationMode = X509RevocationMode.Online,
                RevocationFlag = X509RevocationFlag.EntireChain,
                VerificationTime = asOf.UtcDateTime,
                VerificationFlags = X509VerificationFlags.IgnoreNotTimeNested
            }
        );

        if (status == X509ChainStatusFlags.NoError)
        {
            return new UntrustedValidationResult(signer);
        }

        return new CertificateVerificationFailedValidationResult(signer, asOf, status);
    }
}