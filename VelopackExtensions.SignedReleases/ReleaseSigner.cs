using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class ReleaseSigner
{
    public class Options
    {
        public X509Certificate2 Certificate { get; set; }
        public SigningProcessor SigningProcessor { get; set; }
    }

    private readonly ILogger<ReleaseSigner> _logger;
    private readonly X509Certificate2 _certificate;
    private readonly SigningProcessor _signingProcessor;
    private readonly IFeedSerializer _feedSerializer;

    public ReleaseSigner(ILogger<ReleaseSigner> logger, X509Certificate2 certificate, SigningProcessor signingProcessor, IFeedSerializer feedSerializer)
    {
        _logger = logger;
        _certificate = certificate;
        _signingProcessor = signingProcessor;
        _feedSerializer = feedSerializer;
    }

    public ReleaseSigner(ILogger<ReleaseSigner> logger, IFeedSerializer feedSerializer, IOptions<Options> options) : this(
        logger,
        options.Value.Certificate,
        options.Value.SigningProcessor,
        feedSerializer
    )
    {
    }

    public void SignReleaseFile(string releaseFilePath)
    {
        SignedAssetFeed signedFeed;
        using (Stream stream = File.OpenRead(releaseFilePath))
        {
            signedFeed = _feedSerializer.Deserialize<SignedAssetFeed>(stream);
        }

        signedFeed = SignRelease(signedFeed);

        string tempPath = Path.Join(Path.GetDirectoryName(Path.GetFullPath(releaseFilePath)), Path.GetFileName(releaseFilePath) + ".tmp");
        using (Stream stream = File.Create(tempPath))
        {
            _feedSerializer.Serialize(stream, signedFeed);
        }

        File.Move(tempPath, releaseFilePath, overwrite: true);
    }

    public SignedAssetFeed SignRelease(SignedAssetFeed signedFeed)
    {
        string certThumbprint = _certificate.GetCertHashString(HashAlgorithmName.SHA256);
        bool matchedThumbprint = false;
        List<string> certificates = signedFeed.Certificates?.ToList() ?? [];
        foreach (string base64Cert in certificates)
        {
            using var loaded = CertificateUtility.ReadCertificateFromBase64(base64Cert, out var loadedThumbprint);
            if (loadedThumbprint == certThumbprint)
            {
                matchedThumbprint = true;
                break;
            }
        }

        if (!matchedThumbprint)
        {
            certificates.Add(Convert.ToBase64String(_certificate.RawData));
        }

        Span<byte> shaBytes = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> signatureBytes = stackalloc byte[_signingProcessor.MaxSignatureSize];

        List<SignedAsset> assets = [];
        foreach(SignedAsset asset in signedFeed.Assets)
        {
            if (!string.IsNullOrEmpty(asset.SignatureBase64))
            {
                assets.Add(asset);
                continue;
            }

            _logger.LogInformation("Signing asset {assetName}", asset.FileName);

            Convert.FromHexString(asset.SHA256, shaBytes, out _, out _);
            var cbSig = _signingProcessor.SignHash(shaBytes, signatureBytes);
            assets.Add(asset with { SignatureBase64 = Convert.ToBase64String(signatureBytes[..cbSig]), CertHash = certThumbprint });
        }

        return new SignedAssetFeed(certificates.ToImmutableList()){Assets = assets};
    }
}