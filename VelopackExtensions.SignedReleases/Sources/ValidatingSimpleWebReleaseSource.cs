using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Velopack;
using Velopack.Sources;

namespace VaettirNet.VelopackExtensions.SignedReleases.Sources;

public class ValidatingSimpleWebReleaseSource : SimpleWebSource
{
    private readonly ReleaseValidator _releaseValidator;

    public class Options
    {
        public string BaseUrl { get; set; }
        public double Timeout { get; set; } = 20;
    }

    public ValidatingSimpleWebReleaseSource(ReleaseValidator releaseValidator, IOptions<Options> options, IFileDownloader downloader = null) : this(releaseValidator, options.Value.BaseUrl, downloader, options.Value.Timeout)
    {
    }

    public ValidatingSimpleWebReleaseSource(ReleaseValidator releaseValidator, [NotNull] string baseUrl, [CanBeNull] IFileDownloader downloader = null, double timeout = 30) : this(releaseValidator, NormalizeUri(baseUrl), downloader, timeout)
    {
    }

    public ValidatingSimpleWebReleaseSource(ReleaseValidator releaseValidator, [NotNull] Uri baseUri, [CanBeNull] IFileDownloader downloader = null, double timeout = 30) : base(NormalizeUri(baseUri), downloader, timeout)
    {
        _releaseValidator = releaseValidator;
    }

    private static Uri NormalizeUri(string uri) => NormalizeUri(new Uri(uri, UriKind.Absolute));

    private static Uri NormalizeUri(Uri raw)
    {
        UriBuilder b = new (raw);
        if (!b.Path.EndsWith('/'))
        {
            b.Path += '/';
            return b.Uri;
        }

        return raw;
    }

    public override async Task<VelopackAssetFeed> GetReleaseFeed(ILogger logger, string channel, Guid? stagingId = null, VelopackAsset latestLocalRelease = null)
    { 
        string releaseFilename = VelopackUtilities.GetVeloReleaseIndexName(channel);
        var uri = new Uri(BaseUri, releaseFilename);
        
        if (VelopackRuntimeInfo.SystemArch != RuntimeCpu.Unknown) {
            uri = uri.WithQueryParameter("arch", VelopackRuntimeInfo.SystemArch.ToString());
        }

        if (VelopackRuntimeInfo.SystemOs != RuntimeOs.Unknown) {
            uri = uri.WithQueryParameter("os", VelopackRuntimeInfo.SystemOs.GetOsShortName());
            uri = uri.WithQueryParameter("rid", VelopackRuntimeInfo.SystemRid);
        }

        if (latestLocalRelease != null) {
            uri = uri.WithQueryParameter("id", latestLocalRelease.PackageId);
            uri = uri.WithQueryParameter("localVersion", latestLocalRelease.Version.ToString());
        }

        logger.LogInformation("Downloading release file '{releaseFilename}' from '{uri}'.", releaseFilename, uri.AbsoluteUri);

        byte[] json = await Downloader.DownloadBytes(uri.AbsoluteUri, timeout: Timeout).ConfigureAwait(false);
        
        return _releaseValidator.ValidateReleaseFile(json);
    }
}