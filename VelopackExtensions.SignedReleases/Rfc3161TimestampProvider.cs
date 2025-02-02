using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VaettirNet.VelopackExtensions.SignedReleases.Services;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class Rfc3161TimestampProvider : ISignedTimestampService
{
    private readonly ILogger _logger;
    public string TimestampAuthorityUrl { get; }

    public class Options
    {
        public string TimestampAuthorityUrl { get; set; }
    }
    
    public Rfc3161TimestampProvider(string timestampAuthorityUrl, ILogger logger)
    {
        _logger = logger;
        TimestampAuthorityUrl = timestampAuthorityUrl;
    }
    
    public Rfc3161TimestampProvider(IOptions<Options> options, ILogger<Rfc3161TimestampProvider> logger = null) : this(options.Value.TimestampAuthorityUrl, logger)
    {
    }

    public async Task<byte[]> TrySignTimestampAsync(ReadOnlyMemory<byte> hash, HashAlgorithmName hashAlgorithmName)
    {
        var request = Rfc3161TimestampRequest.CreateFromHash(hash, hashAlgorithmName);
        using HttpClient client = new();
        using HttpRequestMessage httpReq = new(HttpMethod.Post, TimestampAuthorityUrl)
        {
            Content = new ByteArrayContent(request.Encode()),
        };
        
        AssemblyName name = Assembly.GetExecutingAssembly().GetName();
        httpReq.Headers.UserAgent.Add(new ProductInfoHeaderValue(name.Name, name.Version.ToString()));
        httpReq.Content.Headers.ContentType = new MediaTypeHeaderValue("application/timestamp-query");
        
        _logger?.LogInformation("Sending TSR to {tsaUrl} for hash {hash}", TimestampAuthorityUrl, new Base64HashFormatter(hash));
        HttpResponseMessage httpResp = await client.SendAsync(httpReq);
        _logger?.LogInformation("Response status code ({nubmericResponseCode}) {httpResponseCode} {reasonMessage}", (int)httpResp.StatusCode, httpResp.StatusCode, httpResp.ReasonPhrase);
        if (!httpResp.IsSuccessStatusCode)
        {
            return null;
        }

        byte[] bytes = await httpResp.Content.ReadAsByteArrayAsync();
        switch (httpResp.Content.Headers.ContentType?.MediaType)
        {
            case "text/html":
                _logger?.LogError("The server responded with html: {htmlDoc}", Encoding.UTF8.GetString(bytes).Replace("\n", " "));
                throw new CryptographicException("Unexpected response from TSR server");
            case "application/timestamp-reply":
                // This is good
                break;
            case var contentType:
                _logger?.LogError("The server responded with unknown content type: {contentType}", contentType);
                throw new CryptographicException("Unexpected response from TSR server");
        }

        Rfc3161TimestampToken resp = request.ProcessResponse(bytes, out _);
        _logger?.LogDebug("Successfully validated token");
        return resp.AsSignedCms().Encode();
    }

    public bool ValidateTimestampSignature(ReadOnlyMemory<byte> signedTimestamp, ReadOnlySpan<byte> hash, out DateTimeOffset timestamp)
    {
        _logger?.Log(LogLevel.Debug, $"Validation timestamp for {hash}");
        if (!Rfc3161TimestampToken.TryDecode(signedTimestamp, out Rfc3161TimestampToken token, out _))
        {
            _logger?.LogWarning("Invalid timestamp token");
            timestamp = default;
            return false;
        }

        if (!token.VerifySignatureForHash(hash, token.TokenInfo.HashAlgorithmId, out _))
        {
            _logger?.Log(LogLevel.Warning, $"Timestamp signature does not validate for {hash}");
            timestamp = default;
            return false;
        }

        _logger?.Log(LogLevel.Debug, $"Validation successful");
        timestamp = token.TokenInfo.Timestamp;
        return true;
    }

    public DateTimeOffset GetUnverifiedTimestamp(ReadOnlyMemory<byte> signedTimestamp)
    {
        if (!Rfc3161TimestampToken.TryDecode(signedTimestamp, out Rfc3161TimestampToken token, out _))
        {
            throw new ArgumentException("Not a valid timestamp", nameof(signedTimestamp));
        }

        return token.TokenInfo.Timestamp;
    }
}