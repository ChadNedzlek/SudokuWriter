using System;
using System.Security.Cryptography;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public readonly struct PublicKeyHashFormatter
{
    private readonly AsymmetricAlgorithm _alg;

    public PublicKeyHashFormatter(AsymmetricAlgorithm alg)
    {
        _alg = alg;
    }

    public override string ToString() => Convert.ToBase64String(SHA256.HashData(_alg.ExportSubjectPublicKeyInfo()));
}