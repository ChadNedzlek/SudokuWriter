using System;
using System.Security.Cryptography.X509Certificates;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class DefaultTrustResolver : IAssetTrustResolver
{
    private readonly TimeProvider _timeProvider;
    public static readonly DefaultTrustResolver Instance = new(TimeProvider.System);

    public DefaultTrustResolver(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public AssetValidationResult Validate(VelopackAsset asset, X509Certificate2 signer)
    {
        signer.Verify();
        DateTimeOffset now = _timeProvider.GetUtcNow();
        if (signer.NotBefore > now || signer.NotAfter < now)
        {
            return new ExpiredCertificateValidationResult(signer, now);
        }

        return new UntrustedValidationResult(signer);
    }
}