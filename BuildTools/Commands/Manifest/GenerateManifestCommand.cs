using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using VaettirNet.VelopackExtensions.SignedReleases;

namespace VaettirNet.BuildTools.Commands.Manifest;

public class GenerateManifestCommand : CommandBase
{
    public const string BeginManifestBlock = "-----BEGIN MANIFEST BLOCK-----";
    public const string EndManifestBlock = "-----END MANIFEST BLOCK-----";
    public const string AggregateHashBlockLabel = "AGGREGATE HASH";
    public const string SignatureEcdsaBlockLabel = "SIGNATURE ECDSA";
    public const string SignatureRsaBlockLabel = "SIGNATURE RSA";

    private readonly ILogger<GenerateManifestCommand> _logger;
    public string PrivateKeyFile { get; private set;}
    public string CertificateFile { get; private set;}
    public string InputDirectory { get; private set;}
    public string OutputFile { get; private set;}
    
    public GenerateManifestCommand(ILogger<GenerateManifestCommand> logger) : base("manifest gen", "Generate a manifest for a directory of files, signing the result")
    {
        _logger = logger;
        Options = new()
        {
            {"key|k=", "Private key file for signing", v => PrivateKeyFile = v},
            {"certificate|cert|c=", "Certificate file to include in manifest for later validation", v => CertificateFile = v},
            {"directory|dir|d=", "Directory to sign", v => InputDirectory = v},
            {"output|out|o=", "Output file (default: directory/manifest)", v => OutputFile = v},
        };
    }

    protected override int Execute()
    {
        ValidateRequiredArgument(PrivateKeyFile, "key");
        ValidateRequiredArgument(InputDirectory, "directory");

        if (string.IsNullOrEmpty(OutputFile))
        {
            OutputFile = Path.Join(InputDirectory, "manifest");
        }

        using var manifestStream = File.Create(OutputFile);
        using var writer = new StreamWriter(manifestStream) { NewLine = "\n" };
        writer.WriteLine(BeginManifestBlock);
        var manifestHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        foreach (var filename in Directory.EnumerateFiles(InputDirectory, "*.*", SearchOption.AllDirectories).OrderBy(s => s))
        {
            if (OutputFile.Equals(filename, StringComparison.OrdinalIgnoreCase)) continue;
            var relPath = Path.GetRelativePath(InputDirectory, filename);
            relPath = relPath.Replace('\\', '/');
            CommandSet.Out.WriteLine($"Hashing '{relPath}'...");
            using var fileStream = File.OpenRead(filename);
            var length = fileStream.Length;
            SHA256.HashData(fileStream, hash);
            writer.WriteLine($"{length}\t{Convert.ToHexString(hash)}\t{relPath}");
            manifestHash.AppendData(hash);
        }
        writer.WriteLine(EndManifestBlock);
        CommandSet.Out.WriteLine("Finalizing manifest...");

        manifestHash.GetHashAndReset(hash);
        writer.WriteLine(PemEncoding.WriteString(AggregateHashBlockLabel, hash));
        Span<byte> sig = stackalloc byte[500];
        var label = Sign(hash, sig, out int cbSig, out string publicKey, out var cert);
        if (cert != null)
        {
            writer.WriteLine(cert.ExportCertificatePem());
        }
        else
        {
            writer.WriteLine(publicKey);
        }

        writer.WriteLine(PemEncoding.WriteString(label, sig[..cbSig]));
        CommandSet.Out.WriteLine("Done.");
        return 0;
    }

    private string Sign(Span<byte> hash, Span<byte> signature, out int cbSignature, out string publicKey, out X509Certificate2 cert)
    {
        _logger.LogDebug("Reading private key file");
        string pfxFile = Path.GetExtension(PrivateKeyFile) == ".pfx" ? PrivateKeyFile :
            Path.GetExtension(CertificateFile) == ".pfx" ? CertificateFile : null;
        if (pfxFile != null)
        {
            X509Certificate2 pfxCert = X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(PrivateKeyFile), null);
            if (pfxCert.GetECDsaPrivateKey() is { } ecdsa)
            {
                _logger.LogInformation("Loaded ecdsa key with public key {publicKey}", new PublicKeyHashFormatter(ecdsa));
                cbSignature = ecdsa.SignHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence);
                publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();
                cert = pfxCert;
                return SignatureEcdsaBlockLabel;
            }
            if (pfxCert.GetRSAPrivateKey() is { } rsa)
            {
                _logger.LogInformation("Loaded rsa key with public key {publicKey}", new PublicKeyHashFormatter(rsa));
                cbSignature = rsa.SignHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                publicKey = rsa.ExportSubjectPublicKeyInfoPem();
                cert = pfxCert;
                return SignatureRsaBlockLabel;
            }

            throw new CommandFailedException("Invalid PFX file specified", 3);
        }

        if (!string.IsNullOrEmpty(CertificateFile))
        {
            cert = X509CertificateLoader.LoadCertificate(File.ReadAllBytes(CertificateFile));
            _logger.LogInformation("Loaded cert with public key {publicKey}", new PublicKeyHashFormatter(cert.GetECDsaPublicKey() ?? (AsymmetricAlgorithm)cert.GetRSAPublicKey()));
        }
        else
        {
            _logger.LogInformation("No certificate supplied");
            cert = null;
        }

        ReadOnlySpan<char> keyString = File.ReadAllText(PrivateKeyFile);
        var field = PemEncoding.Find(keyString);
        switch (keyString[field.Label])
        {
            case "EC PRIVATE KEY":
                SignEcDsa(keyString[field.Base64Data], field.DecodedDataLength, hash, signature, out cbSignature, out publicKey);
                return SignatureEcdsaBlockLabel;
            case "RSA PRIVATE KEY":
                SignRsa(keyString[field.Base64Data], field.DecodedDataLength, hash, signature, out cbSignature, out publicKey);
                return SignatureRsaBlockLabel;
            default:
                throw new CommandFailedException($"private key file contains unknown key type '{keyString[field.Label]}'", 3);
        }
    }

    private void SignEcDsa(
        ReadOnlySpan<char> text,
        int length,
        ReadOnlySpan<byte> hash,
        Span<byte> signature,
        out int cbSignature,
        out string publicKey
    )
    {
        _logger.LogInformation("Importing {size} bytes of ECDsa key from file", length);
        Span<byte> bytes = stackalloc byte[length];
        if (!Convert.TryFromBase64Chars(text, bytes, out int cbBytes) || cbBytes != length)
        {
            _logger.LogError("Failed to decode base 64 data");
            throw new CommandFailedException("Invalid key file", 4);
        }

        var ecdsa = ECDsa.Create();
        ecdsa.ImportECPrivateKey(bytes, out _);
        _logger.LogInformation("Loaded key with public key {publicKey}", new PublicKeyHashFormatter(ecdsa));
        cbSignature = ecdsa.SignHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence);
        publicKey = ecdsa.ExportSubjectPublicKeyInfoPem();
    }

    private void SignRsa(
        ReadOnlySpan<char> text,
        int length,
        ReadOnlySpan<byte> hash,
        Span<byte> signature,
        out int cbSignature,
        out string publicKey)
    {
        _logger.LogInformation("Importing {size} bytes of RSA key from file", length);
        Span<byte> bytes = stackalloc byte[length];
        if (!Convert.TryFromBase64Chars(text, bytes, out int cbBytes) || cbBytes != length)
        {
            _logger.LogError("Failed to decode base 64 data");
            throw new CommandFailedException("Invalid key file", 4);
        }

        var rsa = RSA.Create();
        rsa.ImportSubjectPublicKeyInfo(bytes, out _);
        _logger.LogInformation("Loaded key with public key {publicKey}", new PublicKeyHashFormatter(rsa));
        cbSignature = rsa.SignHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
        publicKey = rsa.ExportSubjectPublicKeyInfoPem();
    }
}