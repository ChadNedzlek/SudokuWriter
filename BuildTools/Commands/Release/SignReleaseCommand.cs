using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.BuildTools.Commands.Release;

public class SignReleaseCommand : CommandBase
{
    private readonly ReleaseSignerFactory _signerFactory;
    public string CertificateFilePath { get; private set; }
    public string PrivateKeyFilePath { get; private set; }
    public string ReleaseFilePath { get; private set; }

    public SignReleaseCommand(ReleaseSignerFactory signerFactory) : base("vpk release sign", "Sign a velopack releases.json file")
    {
        _signerFactory = signerFactory;
        Options = new()
        {
            {"release|rel|r=", "Velopack release.json file path", v => ReleaseFilePath = v},
            {"private-key|key|k=", "Private key (or pfx) file.", v => PrivateKeyFilePath = v},
            {"certificate|cert|c=", "Certificate file (optional if pfx is used for key)", v => CertificateFilePath = v},
        };
    }

    protected override int Execute()
    {
        ValidateRequiredArgument(ReleaseFilePath, "release");
        ValidateRequiredArgument(PrivateKeyFilePath, "private-key");

        X509Certificate2 cert = null;
        SigningProcessor signingProcessor;
        if (Path.GetExtension(PrivateKeyFilePath)?.ToLowerInvariant() == ".pfx")
        {
            cert = X509CertificateLoader.LoadPkcs12FromFile(PrivateKeyFilePath, null, X509KeyStorageFlags.EphemeralKeySet);
            signingProcessor = SigningProcessor.FromCertificate(cert);
        }
        else
        {
            var keyText = File.ReadAllText(PrivateKeyFilePath).AsSpan();
            signingProcessor = SigningProcessor.FromPem(keyText);
        }

        if (cert == null)
        {
            ValidateRequiredArgument(CertificateFilePath, "certificate");
            cert = X509CertificateLoader.LoadCertificate(File.ReadAllBytes(CertificateFilePath));
        }

        ReleaseSigner signer = _signerFactory.Create(cert, signingProcessor);

        if (Directory.Exists(ReleaseFilePath))
        {
            foreach(var file in Directory.GetFiles(ReleaseFilePath, "releases.*.json", SearchOption.TopDirectoryOnly))
            {
                signer.SignReleaseFile(file);
            }
            return 0;
        }

        signer.SignReleaseFile(ReleaseFilePath);
        return 0;
    }
}