using System;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public static class CertificateUtility
{
    public static X509Certificate2 ReadCertificateFromBase64(ReadOnlySpan<char> base64)
    {
        Span<byte> cert = stackalloc byte[500];
        if (!Convert.TryFromBase64Chars(base64, cert, out var cbCert))
        {
            cert = new byte[10000];
            if (!Convert.TryFromBase64Chars(base64, cert, out cbCert))
            {
                throw new ArgumentException("Invalid base64", nameof(base64));
            }
        }

        cert = cert[..cbCert];

        return X509CertificateLoader.LoadCertificate(cert);
    }
    
    public static X509Certificate2 ReadCertificateFromBase64(ReadOnlySpan<char> base64, out string thumbprint)
    {
        var certificate = ReadCertificateFromBase64(base64);
        thumbprint = certificate.GetCertHashString(HashAlgorithmName.SHA256);
        return certificate;
    }
    
    public static (X509Certificate2 certificate, string thumbprint) ReadCertificateAndThumbprintFromBase64(ReadOnlySpan<char> base64)
    {
        X509Certificate2 cert = ReadCertificateFromBase64(base64, out var thumbprint);
        return (cert, thumbprint);
    }
}