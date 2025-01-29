using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace VaettirNet.SudokuWriter.Gui;

public interface IAssetSignatureValidator
{
    bool ValidateAssetHash(Span<byte> assetSha256Hash, Span<byte> spanSignature, X509Certificate2 certificate);
}

public class GitHubReleaseOptions
{
    public string RepoUrl { get; set; }
    public string AccessToken { get; set; }
    public bool Prerelease { get; set; }
}


public class ValidatingGitHubReleaseSource : GithubSource
{
    private readonly IAssetSignatureValidator _signatureValidator;

    public ValidatingGitHubReleaseSource(
        IAssetSignatureValidator signatureValidator,
        IOptions<GitHubReleaseOptions> options,
        IFileDownloader downloader = null
    ) : base(options.Value.RepoUrl, options.Value.AccessToken, options.Value.Prerelease, downloader)
    {
        _signatureValidator = signatureValidator;
    }

    public ValidatingGitHubReleaseSource(
        [NotNull] string repoUrl,
        [CanBeNull] string accessToken,
        bool prerelease,
        [CanBeNull] IFileDownloader downloader = null) : base(repoUrl, accessToken, prerelease, downloader)
    {
    }

    /// <inheritdoc />
    public override async Task<VelopackAssetFeed> GetReleaseFeed(ILogger logger, string channel, Guid? stagingId = null, VelopackAsset? latestLocalRelease = null)
    {
        GithubRelease[] releases = await GetReleases(Prerelease);
        if (releases is not { Length: > 0 })
        {
            logger.LogWarning("No releases found at '{RepoUri}'.", RepoUri);
            return new VelopackAssetFeed();
        }

        string releasesFileName = GetVeloReleaseIndexName(channel);
        List<VelopackAsset> entries = [];

        byte[] hash = new byte[SHA256.HashSizeInBytes];
        byte[] sig = new byte[500];
        foreach (var r in releases) {
            // this might be a browser url or an api url (depending on whether we have a AccessToken or not)
            // https://docs.github.com/en/rest/reference/releases#get-a-release-asset
            string assetUrl;
            try {
                assetUrl = GetAssetUrlFromName(r, releasesFileName);
            } catch (Exception ex) {
                logger.LogTrace(ex, "Failed to get asset url");
                continue;
            }
            byte[] releaseBytes = await Downloader.DownloadBytes(assetUrl, Authorization, "application/octet-stream");
            var signedFeed = JsonSerializer.Deserialize<SignedAssetFeed>(releaseBytes);
            List<X509Certificate2> certs = signedFeed.Certificates.Select(ReadCert).ToList();

            foreach (var asset in signedFeed.Assets)
            {
                Convert.FromHexString(asset.SHA256, hash, out _, out _);
                string msg = null;
                X509Certificate2 cert = certs[asset.CertIndex];
                if (asset.SignatureBase64 is null)
                {
                    msg = "Unsigned asset present in feed";
                }
                else if (!Convert.TryFromBase64Chars(asset.SignatureBase64, sig, out var cbSig))
                {
                    msg = ("Invalid base 64 signature asset present in feed");
                }
                else if (!_signatureValidator.ValidateAssetHash(hash, sig.AsSpan(0, cbSig), cert))
                {
                    msg = ("Incorrect signature in asset hash");
                }

                entries.Add(new ValidatedSignedAsset(asset.SignatureBase64, cert, msg is null, msg));
            }
        }

        return new VelopackAssetFeed {
            Assets = entries.ToArray(),
        };
    }

    private X509Certificate2 ReadCert(string base64Cert)
    {
        Span<byte> bytes = stackalloc byte[1000];
        if (!Convert.TryFromBase64Chars(base64Cert, bytes, out int cbCert))
        {
            throw new InvalidDataException();
        }

        return X509CertificateLoader.LoadCertificate(bytes[..cbCert]);
    }

    public static string GetVeloReleaseIndexName(string channel)
    {
        return $"releases.{channel ?? VelopackRuntimeInfo.SystemOs.GetOsShortName()}.json";
    }

    private record SignedAsset(string SignatureBase64, int CertIndex) : VelopackAsset;

    private record SignedAssetFeed
    {
        public string[] Certificates { get; set; } = Array.Empty<string>();
        public SignedAsset[] Assets { get; set; } = Array.Empty<SignedAsset>();
    }
}

public record ValidatedSignedAsset(string SignatureBase64, X509Certificate2 Certificate, bool Validated, string InvalidMessage) : VelopackAsset;