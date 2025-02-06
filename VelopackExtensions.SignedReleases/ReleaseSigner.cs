using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Services;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases;

[InterpolatedStringHandler]
public class DebugBase64FormattedString : Base64FormattedString
{
    public DebugBase64FormattedString(int literalLength, int formattedCount, ILogger logger) : base(literalLength, formattedCount, logger, LogLevel.Debug)
    {
    }
}

[InterpolatedStringHandler]
public class Base64FormattedString
{
    private readonly StringBuilder _builder;
    
    public Base64FormattedString(int literalLength, int formattedCount, ILogger logger, LogLevel level)
    {
        if (logger.IsEnabled(level))
        {
            _builder = new();
        }
    }

    public void AppendLiteral(string part)
    {
        _builder?.Append(part);
    }

    public void AppendFormatted(ReadOnlySpan<byte> bytes)
    {
        if (_builder is { } b)
        {
            Span<char> chunk = stackalloc char[Base64.GetMaxEncodedToUtf8Length(bytes.Length)];
            Convert.TryToBase64Chars(bytes, chunk, out int written);
            b.Append(chunk[..written]);
        }
    }
    
    public void AppendFormatted<T>(T value)
    {
        if (_builder is { } b)
        {
            b.Append(b);
        }
    }
    
    public void AppendFormatted<T>(T value, string format, [CanBeNull] IFormatProvider formatProvider = null) where T : IFormattable
    {
        if (_builder is { } b)
        {
            b.Append(value.ToString(format, formatProvider));
        }
    }
}

public static class LoggerExtensions
{
    public static void Log(this ILogger logger, LogLevel level, [InterpolatedStringHandlerArgument(nameof(logger), nameof(level))] Base64FormattedString message)
    {
        if (logger.IsEnabled(level))
            logger.Log(level, message.ToString());
    }
    public static void LogDebug(this ILogger logger, LogLevel level, [InterpolatedStringHandlerArgument(nameof(logger), nameof(level))] DebugBase64FormattedString message)
    {
        if (logger.IsEnabled(level))
            logger.Log(level, message.ToString());
    }
}

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
    private readonly ISignedTimestampService _timestampService;

    public ReleaseSigner(ILogger<ReleaseSigner> logger, X509Certificate2 certificate, SigningProcessor signingProcessor, IFeedSerializer feedSerializer, ISignedTimestampService timestampService = null)
    {
        _logger = logger;
        _certificate = certificate;
        _signingProcessor = signingProcessor;
        _feedSerializer = feedSerializer;
        _timestampService = timestampService;
    }

    public ReleaseSigner(ILogger<ReleaseSigner> logger, IFeedSerializer feedSerializer, IOptions<Options> options, ISignedTimestampService timestampService = null) : this(
        logger,
        options.Value.Certificate,
        options.Value.SigningProcessor,
        feedSerializer,
        timestampService
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
        IncrementalHash tsrHash = _timestampService is null ? null : IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

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
            tsrHash?.AppendData(shaBytes);
            int cbSig = _signingProcessor.SignHash(shaBytes, signatureBytes);
            assets.Add(asset with { SignatureBase64 = Convert.ToBase64String(signatureBytes[..cbSig]), CertHash = certThumbprint });
        }

        byte [] timestamp = null;
        if (tsrHash != null)
        {
            timestamp = _timestampService.TrySignTimestampAsync(tsrHash).GetAwaiter().GetResult();
            if (timestamp == null)
            {
                throw new CryptographicException("Failed to generate signed timestamp request for data");
            }
        }

        string timestampString = timestamp != null ? Convert.ToBase64String(timestamp) : null;
        return new SignedAssetFeed(certificates.ToImmutableList()) { Assets = assets, TimeStamp = timestampString };
    }
}