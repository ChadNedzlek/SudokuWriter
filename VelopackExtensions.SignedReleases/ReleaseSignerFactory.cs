using System;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.DependencyInjection;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class ReleaseSignerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public ReleaseSignerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ReleaseSigner Create(X509Certificate2 certificate, SigningProcessor signingProcessor) =>
        ActivatorUtilities.CreateInstance<ReleaseSigner>(_serviceProvider, certificate, signingProcessor);
}