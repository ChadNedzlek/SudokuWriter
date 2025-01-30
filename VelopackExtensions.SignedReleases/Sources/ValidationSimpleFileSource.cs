using System;
using System.IO;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace VaettirNet.VelopackExtensions.SignedReleases.Sources;

public class ValidationSimpleFileSource : SimpleFileSource
{
    private readonly ReleaseValidator _releaseValidator;

    public class Options
    {
        public string ReleaseDirectory { get; set; }
    }

    public ValidationSimpleFileSource(ReleaseValidator releaseValidator, [NotNull] DirectoryInfo baseDirectory) : base(baseDirectory)
    {
        _releaseValidator = releaseValidator;
    }

    public ValidationSimpleFileSource(ReleaseValidator releaseValidator, IOptions<Options> options) : this(releaseValidator, new DirectoryInfo(options.Value.ReleaseDirectory))
    {
    }

    public override Task<VelopackAssetFeed> GetReleaseFeed(
        ILogger logger,
        string channel,
        Guid? stagingId = null,
        VelopackAsset latestLocalRelease = null
    )
    {
        if (!BaseDirectory.Exists)
        {
            logger.LogError($"The local update directory '{BaseDirectory.FullName}' does not exist.");
            return Task.FromResult(new VelopackAssetFeed());
        }

        // if a feed exists in the folder, let's use that.
        var feedLoc = Path.Combine(BaseDirectory.FullName, VelopackUtilities.GetVeloReleaseIndexName(channel));
        if (File.Exists(feedLoc))
        {
            logger.LogError($"Found local file feed at '{feedLoc}'.");
            return Task.FromResult<VelopackAssetFeed>(_releaseValidator.ValidateReleaseFile(File.ReadAllBytes(feedLoc)));
        }

        return base.GetReleaseFeed(logger, channel, stagingId, latestLocalRelease);
    }
}