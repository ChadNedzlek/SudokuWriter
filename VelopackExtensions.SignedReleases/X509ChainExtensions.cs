using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace VaettirNet.VelopackExtensions.SignedReleases;

internal static class X509ChainExtensions
{
    public static X509ChainStatusFlags AllFlags(this X509ChainStatus[] statuses) =>
        statuses.Aggregate(X509ChainStatusFlags.NoError, (f, s) => f | s.Status);
    
    public static X509ChainStatusFlags VerifyChain(this X509Certificate2 cert, X509ChainPolicy policy)
    {
        var chain = new X509Chain { ChainPolicy = policy };
        try
        {
            return chain.Build(cert) ? X509ChainStatusFlags.NoError : chain.ChainStatus.AllFlags();
        }
        finally
        {
            foreach (X509Certificate2 element in chain.ChainElements.Select(c => c.Certificate))
            {
                element.Dispose();
            }
        }
    }
}