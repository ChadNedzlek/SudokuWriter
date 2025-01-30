using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Mono.Options;
using VaettirNet.SudokuWriter.Gui;

namespace VaettirNet.BuildTools;

public class ReleaseCommandSet : CommandSet
{
    public string ReleaseFilePath { get; private set; }

    public ReleaseCommandSet(Converter<string, string> localizer = null) : base("vpk release", localizer)
    {
        Initialize();
    }

    public ReleaseCommandSet(TextWriter output, TextWriter error, Converter<string, string> localizer = null) : base("vpk release", output, error, localizer)
    {
        Initialize();
    }

    private void Initialize()
    {
        Add("release|rel|r=", "Velopack release.json file path", v => ReleaseFilePath = v);
    }
}

public class SignReleaseCommand : CommandBase
{
    public string CertificateFilePath { get; private set; }
    public string PrivateKeyFilePath { get; private set; }

    public SignReleaseCommand() : base("vpk release sign", "Sign a velopack releases.json file")
    {
        Options = new()
        {
            {"private-key|key|k=", "Private key (or pfx) file.", v => PrivateKeyFilePath = v},
            {"certificate|cert|c=", "Certificate file (optional if pfx is used for key)", v => CertificateFilePath = v},
        };
    }

    protected override int Execute()
    {
        ReleaseCommandSet releaseSet = CommandSet as ReleaseCommandSet;
        string releaseFilePath = releaseSet?.ReleaseFilePath;
        ValidateRequiredArgument(releaseFilePath, "release");
        ValidateRequiredArgument(PrivateKeyFilePath, "private-key");

        X509Certificate2 cert = null;
        Signer signer;
        if (Path.GetExtension(PrivateKeyFilePath)?.ToLowerInvariant() == ".pfx")
        {
            cert = X509CertificateLoader.LoadPkcs12FromFile(PrivateKeyFilePath, null, X509KeyStorageFlags.EphemeralKeySet);
            signer = Signer.FromCertificate(cert);
        }
        else
        {
            var keyText = File.ReadAllText(PrivateKeyFilePath).AsSpan();
            signer = Signer.FromPem(keyText);
        }

        if (cert == null)
        {
            ValidateRequiredArgument(CertificateFilePath, "certificate");
            cert = X509CertificateLoader.LoadCertificate(File.ReadAllBytes(CertificateFilePath));
        }


        if (Directory.Exists(releaseFilePath))
        {
            int res = 0;
            foreach(var file in Directory.GetFiles(releaseFilePath, "releases.*.json", SearchOption.TopDirectoryOnly))
            {
                res |= SignReleaseFile(file, cert, signer);
            }

            return res;
        }

        return SignReleaseFile(releaseFilePath, cert, signer);
    }

    private int SignReleaseFile(string releaseFilePath, X509Certificate2 certificate, Signer signer)
    {
        SignedAssetFeed signedFeed;
        using (Stream stream = File.OpenRead(releaseFilePath))
        {
            signedFeed = JsonSerializer.Deserialize<SignedAssetFeed>(stream);
        }

        string certThumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        bool matchedThumbprint = false;
        if (signedFeed.Certificates != null)
        {
            for (int i = 0; i < signedFeed.Certificates.Length; i++)
            {
                var loaded = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(signedFeed.Certificates[i]));
                if (loaded.GetCertHashString(HashAlgorithmName.SHA256) == certThumbprint)
                {
                    matchedThumbprint = true;
                    break;
                }
            }
        }

        if (!matchedThumbprint)
        {
            var existingCerts = signedFeed.Certificates ?? [];
            signedFeed.Certificates = [..existingCerts, Convert.ToBase64String(certificate.RawData)];
        }

        Span<byte> shaBytes = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> signatureBytes = stackalloc byte[signer.MaxSignatureSize];
        for (int i = 0; i < signedFeed.Assets.Length; i++)
        {
            SignedAsset asset = signedFeed.Assets[i];
            if (!string.IsNullOrEmpty(asset.SignatureBase64))
                continue;

            Convert.FromHexString(asset.SHA256, shaBytes, out _, out _);
            var cbSig = signer.SignHash(shaBytes, signatureBytes);
            signedFeed.Assets[i] = asset with { SignatureBase64 = Convert.ToBase64String(signatureBytes[..cbSig]), CertHash = certThumbprint };
        }

        string tempPath = Path.Join(Path.GetDirectoryName(Path.GetFullPath(releaseFilePath)), Path.GetFileNameWithoutExtension(releaseFilePath) + ".tmp.json");
        using (Stream stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, signedFeed, new JsonSerializerOptions
            {
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            });
        }

        File.Move(tempPath, releaseFilePath);
        return 0;
    }
}

public record struct SignatureOptions(RsaSignatureOptions Rsa = null, EcdsaSignatureOptions EcDsa = null);

public record RsaSignatureOptions(HashAlgorithmName HashAlgorithmName, RSASignaturePadding Padding)
{
    public static readonly RsaSignatureOptions Default = new(HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
}

public record EcdsaSignatureOptions(DSASignatureFormat SignatureFormat)
{
    public static readonly EcdsaSignatureOptions Default = new(DSASignatureFormat.Rfc3279DerSequence);
}

public class Signer
{
    private readonly RSA _rsa;
    private readonly SignatureOptions _options;
    private readonly ECDsa _ecDsa;

    public Signer(RSA rsa, RsaSignatureOptions options)
    {
        _rsa = rsa;
        _options = new SignatureOptions(options, null);
    }
    
    public Signer(ECDsa ecDsa, EcdsaSignatureOptions options)
    {
        _ecDsa = ecDsa;
        _options = new SignatureOptions(null, options);
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

    public static Signer FromCertificate(X509Certificate2 cert, SignatureOptions options = default)
    {
        if (cert.GetRSAPrivateKey() is { } rsa) return new(rsa, options.Rsa);
        if (cert.GetECDsaPrivateKey() is { } ecDsa) return new(ecDsa, options.EcDsa);

        throw new ArgumentException("Certificate has no supported private keys");
    }

    public static Signer FromRsaPem(ReadOnlySpan<char> text, RsaSignatureOptions options = null)
    {
        RSA rsa = RSA.Create();
        rsa.ImportFromPem(text);
        return new(rsa, options);
    }
    
    public static Signer FromEcdsaPem(ReadOnlySpan<char> text, EcdsaSignatureOptions options = null)
    {
        var ecdsa = ECDsa.Create();
        ecdsa.ImportFromPem(text);
        return new(ecdsa, options);
    }

    public static Signer FromPem(ReadOnlySpan<char> text, SignatureOptions options = default)
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
}