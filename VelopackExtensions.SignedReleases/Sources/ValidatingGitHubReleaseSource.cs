using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;
using Velopack;
using Velopack.Sources;

namespace VaettirNet.VelopackExtensions.SignedReleases.Sources;

public class ValidatingGitHubReleaseSource : GithubSource
{
    public class Options
    {
        public string RepoUrl { get; set; }
        public string AccessToken { get; set; }
        public bool Prerelease { get; set; }
    }
    
    private readonly ReleaseValidator _releaseValidator;

    public ValidatingGitHubReleaseSource(
        ReleaseValidator releaseValidator,
        IOptions<Options> options,
        IFileDownloader downloader = null
    ) : base(options.Value.RepoUrl, options.Value.AccessToken, options.Value.Prerelease, downloader)
    {
        _releaseValidator = releaseValidator;
    }

    public ValidatingGitHubReleaseSource(
        [NotNull] string repoUrl,
        [CanBeNull] string accessToken,
        bool prerelease,
        [CanBeNull] IFileDownloader downloader = null) : base(repoUrl, accessToken, prerelease, downloader)
    {
    }

    /// <inheritdoc />
    public override async Task<VelopackAssetFeed> GetReleaseFeed(ILogger logger, string channel, Guid? stagingId = null, VelopackAsset latestLocalRelease = null)
    {
        GithubRelease[] releases = await GetReleases(Prerelease);
        if (releases is not { Length: > 0 })
        {
            logger.LogWarning("No releases found at '{RepoUri}'.", RepoUri);
            return new VelopackAssetFeed();
        }

        string releasesFileName = VelopackUtilities.GetVeloReleaseIndexName(channel);
        List<ValidatedGitAsset> entries = [];
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
            var validatedFeed = _releaseValidator.ValidateReleaseFile(releaseBytes);
            entries.AddRange(validatedFeed.Assets.Select(e => new ValidatedGitAsset(e.ValidationResult, e, r)));
        }

        return new VelopackAssetFeed {
            Assets = entries.Cast<VelopackAsset>().ToArray(),
        };
    }

    private record ValidatedGitAsset : GitBaseAsset
    {
        public AssetValidationResult ValidationResult { get; }

        public ValidatedGitAsset(
            AssetValidationResult validationResult,
            VelopackAsset entry,
            GithubRelease release) : base(entry, release)
        {
            ValidationResult = validationResult;
        }
    }
}