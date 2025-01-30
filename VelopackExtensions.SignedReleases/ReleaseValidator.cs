using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class ReleaseValidator
{
    private readonly ILogger<ReleaseSigner> _logger;
    private readonly IFeedSerializer _feedSerializer;
    private readonly IAssetSignatureValidator _signatureValidator;
    private readonly IAssetTrustResolver _trustResolver;

    public ReleaseValidator(ILogger<ReleaseSigner> logger, IFeedSerializer feedSerializer, IAssetSignatureValidator signatureValidator = null, IAssetTrustResolver trustResolver = null)
    {
        _logger = logger;
        _feedSerializer = feedSerializer;
        _signatureValidator = signatureValidator ?? DefaultAssetSignatureValidator.Instance;
        _trustResolver = trustResolver ?? DefaultTrustResolver.Instance;
    }

    public ValidatedAssetFeed ValidateReleaseFile(string releaseFilePath)
    {
        SignedAssetFeed signedFeed;
        using (Stream stream = File.OpenRead(releaseFilePath))
        {
            signedFeed = _feedSerializer.Deserialize<SignedAssetFeed>(stream);
        }

        return ValidateReleaseFile(signedFeed);
    }

    public ValidatedAssetFeed ValidateReleaseFile(SignedAssetFeed signedFeed)
    {
        if (signedFeed.Certificates is not { Count: > 0 } certStrings)
        {
            // With no certs, no signature can be validated
            return new ValidatedAssetFeed { Assets = signedFeed.Assets.Select(a => new ValidatedAsset(UnsignedValidationResult.Instance, a)) };
        }

        Dictionary<string, X509Certificate2> certs = certStrings.Select(str => CertificateUtility.ReadCertificateAndThumbprintFromBase64(str))
            .ToDictionary(p => p.thumbprint, p => p.certificate);
        Dictionary<string, SigningProcessor> signers = certs.ToDictionary(c => c.Key, c => SigningProcessor.FromCertificate(c.Value));
        int maxSize = signers.Values.Max(s => s.MaxSignatureSize);
        Span<byte> shaBytes = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> signatureBytes = stackalloc byte[maxSize];
        List<ValidatedAsset> validated = [];
        foreach (var asset in signedFeed.Assets)
        {
            if (string.IsNullOrEmpty(asset.SignatureBase64))
            {
                validated.Add(new ValidatedAsset(UnsignedValidationResult.Instance, asset));
                continue;
            }

            Convert.FromHexString(asset.SHA256, shaBytes, out _, out _);
            if (!Convert.TryFromBase64Chars(asset.SignatureBase64, signatureBytes, out var cbSig))
            {
                validated.Add(new ValidatedAsset(new InvalidSignatureValidationResult(asset.SignatureBase64), asset));
                continue;
            }

            if (!signers.TryGetValue(asset.CertHash, out var signer) || !certs.TryGetValue(asset.CertHash, out var cert))
            {
                validated.Add(new ValidatedAsset(new UnverifiableValidationResult(asset.CertHash, asset.SignatureBase64), asset));
                continue;
            }

            if (!_signatureValidator.VerifyAssetHash(shaBytes, signatureBytes[..cbSig], signer))
            {
                validated.Add(new ValidatedAsset(new VerificationFailedValidationResult(cert, asset.SignatureBase64), asset));
                continue;
            }
            
            validated.Add(new ValidatedAsset(_trustResolver.Validate(asset, cert), asset));
        }

        return new ValidatedAssetFeed{Assets = validated};
    }

    public ValidatedAssetFeed ValidateReleaseFile(byte[] releaseBytes)
    {
        SignedAssetFeed signedFeed;
        {
            signedFeed = _feedSerializer.Deserialize<SignedAssetFeed>(releaseBytes);
        }

        return ValidateReleaseFile(signedFeed);
    }
}