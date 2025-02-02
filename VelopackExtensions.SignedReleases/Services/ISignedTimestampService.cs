using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace VaettirNet.VelopackExtensions.SignedReleases.Services;

public interface ISignedTimestampService
{
    [ItemCanBeNull]
    Task<byte[]> TrySignTimestampAsync(ReadOnlyMemory<byte> hash, HashAlgorithmName hashAlgorithmName);
    bool ValidateTimestampSignature(ReadOnlyMemory<byte> signedTimestamp, ReadOnlySpan<byte> hash, out DateTimeOffset timestamp);
    DateTimeOffset GetUnverifiedTimestamp(ReadOnlyMemory<byte> signedTimestamp);
}

public static class SignedTimestampService
{
    [ItemCanBeNull]
    public static Task<byte[]> TrySignTimestampAsync([NotNull] this ISignedTimestampService service, [NotNull] IncrementalHash hash)
    {
        ArgumentNullException.ThrowIfNull(hash);
        ArgumentNullException.ThrowIfNull(service);
        return service.TrySignTimestampAsync(hash.GetHashAndReset(), hash.AlgorithmName);
    }
}