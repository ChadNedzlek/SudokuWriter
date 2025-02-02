using System;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public readonly struct Base64HashFormatter
{
    private readonly ReadOnlyMemory<byte> _hash;

    public Base64HashFormatter(ReadOnlyMemory<byte> hash)
    {
        _hash = hash;
    }

    public override string ToString() => Convert.ToBase64String(_hash.Span);
}