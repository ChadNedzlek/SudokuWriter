using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace VaettirNet.BuildTools;

public class VerifyManifestCommand : CommandBase
{
    private readonly ILogger<VerifyManifestCommand> _logger;
    public string SubjectName { get; private set;}
    public string SubjectKeyIdentifier { get; private set;}
    public string AuthorityKeyIdentifier { get; private set;}
    public string InputDirectory { get; private set;}
    public string ManifestFile { get; private set;}
    
    public VerifyManifestCommand(ILogger<VerifyManifestCommand> logger) : base("manifest verify", "Validate a manifest file")
    {
        _logger = logger;
        Options = new()
        {
            {"subject-name|subject|name|n=", "Expected subject name of signature. (optional)", v => SubjectName = v},
            {"subject-key-identify|ski|s=", "Expected subject key identifier of signature. (optional)", v => SubjectKeyIdentifier = v},
            {"authority-key-identify|aki|a=", "Expected authority key identifier of signature. (optional)", v => AuthorityKeyIdentifier = v},
            {"directory|dir|d=", "Directory to verify", v => InputDirectory = v},
            {"manifest|m=", "Manifest file (default: directory/manifest)", v => ManifestFile = v},
        };
    }

    protected override int Execute()
    {
        ValidateRequiredArgument(InputDirectory, "directory");

        if (string.IsNullOrEmpty(ManifestFile))
        {
            ManifestFile = Path.Join(InputDirectory, "manifest");
        }

        ManifestFile = Path.GetFullPath(ManifestFile);
        InputDirectory = Path.GetFullPath(InputDirectory);

        using var manifestStream = File.OpenRead(ManifestFile);
        using var reader = new StreamReader(manifestStream);
        if (reader.ReadLine() != GenerateManifestCommand.BeginManifestBlock)
        {
            CommandSet.Error.WriteLine("Invalid manifest file, missing begin manifest block");
            return 3;
        }

        var manifestHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        Span<byte> compareHash = stackalloc byte[SHA256.HashSizeInBytes];
        HashSet<string> validatedPaths = new(StringComparer.OrdinalIgnoreCase);
        string line; 
        while((line = reader.ReadLine()) is not (null or GenerateManifestCommand.EndManifestBlock))
        {
            ReadOnlySpan<char> lineSpan = line.AsSpan();
            var segments = lineSpan.Split('\t');
            if (!segments.MoveNext() || !uint.TryParse(lineSpan[segments.Current], out uint length))
            {
                CommandSet.Error.WriteLine("Invalid manifest file, file section missing length/delimiter");
                return 3;
            }
            if (!segments.MoveNext())
            {
                CommandSet.Error.WriteLine("Invalid manifest file, file section missing hash/delimiter");
                return 3;
            }

            Convert.FromHexString(lineSpan[segments.Current], hash, out _, out _);
            if (!segments.MoveNext())
            {
                CommandSet.Error.WriteLine("Invalid manifest file, file section missing file path");
                return 3;
            }

            ReadOnlySpan<char> pathSpan = lineSpan[segments.Current.Start..];
            string fileName = Path.GetFullPath(Path.Join(InputDirectory, pathSpan));
            if (ManifestFile.Equals(fileName, StringComparison.OrdinalIgnoreCase)) continue;
            
            using var fileStream = File.OpenRead(fileName);
            if (fileStream.Length != length)
            {
                CommandSet.Error.WriteLine($"Length mismatch for {pathSpan}");
                return 4;
            }

            SHA256.HashData(fileStream, compareHash);
            if (!hash.SequenceEqual(compareHash))
            {
                CommandSet.Error.WriteLine($"Hash validation failed for {pathSpan}");
                return 4;
            }

            validatedPaths.Add(pathSpan.ToString());
            manifestHash.AppendData(hash);
        }

        if (line == null)
        {
            CommandSet.Error.WriteLine("Invalid manifest file, missing end manifest block");
            return 3;
        }

        foreach (var filename in Directory.EnumerateFiles(InputDirectory, "*.*", SearchOption.AllDirectories).OrderBy(s => s))
        {
            if (ManifestFile.Equals(filename, StringComparison.OrdinalIgnoreCase)) continue;
            var relPath = Path.GetRelativePath(InputDirectory, filename).Replace('\\', '/');
            if (!validatedPaths.Contains(relPath))
            {
                CommandSet.Error.WriteLine($"Extra file found {relPath}");
                return 4;
            }
        }

        manifestHash.GetHashAndReset(hash);

        ReadOnlySpan<char> rem = reader.ReadToEnd();
        ReadOnlySpan<char> publicKeySpan = default;
        Span<byte> signature = stackalloc byte[500];
        bool validatedHash = false;
        bool validatedSignature = false;
        AsymmetricAlgorithm publicKey = null;
        while (PemEncoding.TryFind(rem, out var field))
        {
            switch (rem[field.Label])
            {
                case GenerateManifestCommand.AggregateHashBlockLabel:
                {
                    if (field.DecodedDataLength != compareHash.Length || !Convert.TryFromBase64Chars(rem[field.Base64Data], compareHash, out _))
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, malformed hash");
                        return 3;
                    }
                    
                    if (!hash.SequenceEqual(compareHash))
                    {
                        CommandSet.Error.WriteLine("Hash validation failed for aggregate hash");
                        return 4;
                    }

                    validatedHash = true;
                    break;
                }
                case "PUBLIC KEY":
                {
                    publicKeySpan = rem[field.Location];
                    break;
                }
                case "CERTIFICATE":
                {
                    Span<byte> bytes = new byte[field.DecodedDataLength];
                    if (!Convert.TryFromBase64Chars(rem[field.Base64Data], bytes, out _))
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, malformed certificate");
                        return 3;
                    }

                    var cert = X509CertificateLoader.LoadCertificate(bytes);
                    publicKey = cert.GetRSAPublicKey() ?? (AsymmetricAlgorithm)cert.GetECDsaPublicKey();

                    if (!string.IsNullOrEmpty(SubjectName))
                    {
                        var expected = new X500DistinguishedName(SubjectName).Decode(
                            X500DistinguishedNameFlags.UseCommas | X500DistinguishedNameFlags.ForceUTF8Encoding
                        );
                        var inCert = cert.SubjectName.Decode(
                            X500DistinguishedNameFlags.UseCommas | X500DistinguishedNameFlags.ForceUTF8Encoding
                        );

                        if (expected != inCert)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate does not have matching Subject: {inCert}");
                            return 4;
                        }
                    }

                    if (!string.IsNullOrEmpty(AuthorityKeyIdentifier))
                    {
                        if (cert.Extensions.OfType<X509AuthorityKeyIdentifierExtension>().FirstOrDefault() is not { } aki)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate does not authority key info");
                            return 4;
                        }

                        if (aki.KeyIdentifier is null)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate authority key identifier has no key id");
                            return 4;
                        }

                        Span<byte> expectedAki = stackalloc byte[100];
                        Convert.FromHexString(AuthorityKeyIdentifier, expectedAki, out _, out var cbAki);

                        expectedAki = expectedAki[..cbAki];

                        if (aki.KeyIdentifier is not {} akiki)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate authority key identifier has no key id");
                            return 4;
                        }

                        if (!expectedAki.SequenceEqual(akiki.Span))
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate does not have matching authority key identifier: {Convert.ToHexString(akiki.Span)}");
                            return 4;
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(SubjectKeyIdentifier))
                    {
                        if (cert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault() is not { } ski)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate does not subject key info");
                            return 4;
                        }

                        if (ski.SubjectKeyIdentifier != SubjectKeyIdentifier)
                        {
                            CommandSet.Error.WriteLine($"Embedded certificate does not have matching subject key id: {ski.SubjectKeyIdentifier}");
                            return 4;
                        }
                    }

                    break;
                }
                    
                case GenerateManifestCommand.SignatureRsaBlockLabel:
                {
                    if (!Convert.TryFromBase64Chars(rem[field.Base64Data], signature, out int cbSignature))
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, invalid rsa public key");
                        return 3;
                    }
                    signature = signature[..cbSignature];
                    if (publicKey != null)
                        break;
                    
                    if (publicKeySpan.IsEmpty)
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, missing public key");
                        return 3;
                    }

                    if (!validatedHash)
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, missing aggregate hash");
                        return 3;
                    }

                    RSA rsa = RSA.Create();
                    rsa.ImportFromPem(publicKeySpan);
                    publicKey = rsa;
                    break;
                }
                case GenerateManifestCommand.SignatureEcdsaBlockLabel:
                {
                    if (!Convert.TryFromBase64Chars(rem[field.Base64Data], signature, out int cbSignature))
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, invalid rsa public key");
                        return 3;
                    }
                    signature = signature[..cbSignature];
                    if (publicKey != null)
                        break;
                    
                    if (publicKeySpan.IsEmpty)
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, missing public key");
                        return 3;
                    }

                    if (!validatedHash)
                    {
                        CommandSet.Error.WriteLine("Invalid manifest file, missing aggregate hash");
                        return 3;
                    }

                    ECDsa ecdsa = ECDsa.Create();
                    ecdsa.ImportFromPem(publicKeySpan);
                    publicKey = ecdsa;
                    break;
                }

            }
            rem = rem[field.Location.End..];
        }

        if (!validatedHash)
        {
            CommandSet.Error.WriteLine("Invalid manifest file, missing aggregate hash");
            return 3;
        }

        switch (publicKey)
        {
            case RSA rsa:
                if (!rsa.VerifyHash(hash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pss))
                {
                    CommandSet.Error.WriteLine("Signature validation failed");
                    return 4;
                }
                validatedSignature = true;
                break;
            case ECDsa ecdsa:
                if (!ecdsa.VerifyHash(hash, signature, DSASignatureFormat.Rfc3279DerSequence))
                {
                    CommandSet.Error.WriteLine("Signature validation failed");
                    return 4;
                }
                validatedSignature = true;
                break;
        }

        if (!validatedSignature)
        {
            CommandSet.Error.WriteLine("Invalid manifest file, missing signature block");
            return 3;
        }

        CommandSet.Out.WriteLine("Validation successful.");
        return 0;
    }
}