using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases.Signing;

public class SigningProcessor : IDisposable
{
    private readonly RSA _rsa;
    private readonly SignatureOptions _options;
    private readonly ECDsa _ecDsa;

    public SigningProcessor(RSA rsa, RsaSignatureOptions options)
    {
        _rsa = rsa;
        _options = new SignatureOptions(Rsa: options);
    }
    
    public SigningProcessor(ECDsa ecDsa, EcdsaSignatureOptions options)
    {
        _ecDsa = ecDsa;
        _options = new SignatureOptions(EcDsa: options);
    }

    public int MaxSignatureSize => _rsa?.GetMaxOutputSize()
        ?? _ecDsa?.GetMaxSignatureSize(EcDsaOptions.SignatureFormat)
        ?? throw new InvalidOperationException();
    
    private RsaSignatureOptions RsaOptions => _options.Rsa ?? RsaSignatureOptions.Default;
    private EcdsaSignatureOptions EcDsaOptions => _options.EcDsa ?? EcdsaSignatureOptions.Default;

    public int SignHash(ReadOnlySpan<byte> hash, Span<byte> signature)
    {
        if (_rsa is not null)
        {
            return _rsa.SignHash(hash, signature, RsaOptions.HashAlgorithmName, RsaOptions.Padding);
        }

        return _ecDsa.SignHash(hash, signature, EcDsaOptions.SignatureFormat);
    }
    
    public byte[] SignHash(ReadOnlySpan<byte> hash)
    {
        if (_rsa is not null)
        {
            return _rsa.SignHash(hash, RsaOptions.HashAlgorithmName, RsaOptions.Padding);
        }

        return _ecDsa.SignHash(hash, EcDsaOptions.SignatureFormat);
    }

    public bool VerifyHash(ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
    {
        if (_rsa is not null) return _rsa.VerifyHash(hash, signature, RsaOptions.HashAlgorithmName, RsaOptions.Padding);
        if (_ecDsa is not null) return _ecDsa.VerifyHash(hash, signature, EcDsaOptions.SignatureFormat);
        throw new InvalidOperationException("No matching algorithm");
    }

    public static SigningProcessor FromCertificate(X509Certificate2 cert, SignatureOptions options = default)
    {
        if (cert.GetRSAPrivateKey() is { } rsa) return new(rsa, options.Rsa);
        if (cert.GetECDsaPrivateKey() is { } ecDsa) return new(ecDsa, options.EcDsa);
        if (cert.GetRSAPublicKey() is { } rsaPublic) return new(rsaPublic, options.Rsa);
        if (cert.GetECDsaPublicKey() is { } ecDsaPublic) return new(ecDsaPublic, options.EcDsa);

        throw new ArgumentException("Certificate has no supported private keys");
    }

    public static SigningProcessor FromRsaPem(ReadOnlySpan<char> text, RsaSignatureOptions options = null)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(text);
        return new(rsa, options);
    }
    
    public static SigningProcessor FromEcdsaPem(ReadOnlySpan<char> text, EcdsaSignatureOptions options = null)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(text);
        return new(ecdsa, options);
    }

    public static SigningProcessor FromPem(ReadOnlySpan<char> text, SignatureOptions options = default)
    {
        if (!PemEncoding.TryFind(text, out var field))
        {
            throw new ArgumentException("private-key is not a valid PEM file");
        }
        
        return text[field.Label] switch
        {
            "RSA PRIVATE KEY" => FromRsaPem(text, options.Rsa),
            "EC PRIVATE KEY" => FromEcdsaPem(text, options.EcDsa),
            _ => throw new ArgumentException($"Unknown key format: {text[field.Label]}"),
        };
    }

    public void Dispose()
    {
        _rsa?.Dispose();
        _ecDsa?.Dispose();
    }
}