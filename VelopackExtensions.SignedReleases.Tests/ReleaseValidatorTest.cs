using System;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Shouldly;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

namespace VaettirNet.VelopackExtensions.SignedReleases.Tests;

[TestFixture]
[TestOf(typeof(ReleaseValidator))]
public class ReleaseValidatorTest
{
    private static readonly ECDsa s_alg = ECDsa.Create();

    private static readonly X509Certificate2 s_testCert =
        new CertificateRequest("CN=Test", s_alg, HashAlgorithmName.SHA256)
            .CreateSelfSigned(
                DateTimeOffset.UtcNow.AddDays(1),
                DateTimeOffset.UtcNow.AddDays(1)
            );

    private static readonly string s_certString = Convert.ToBase64String(s_testCert.RawData);

    private readonly ReleaseValidator _releaseValidator;
    
    public ReleaseValidatorTest()
    {
        LoggerFactory f = new LoggerFactory([NUnitLogger.Instance]);
        _releaseValidator = new ReleaseValidator(
            f.CreateLogger<ReleaseValidator>(),
            DefaultFeedSerializer.Basic,
            new DefaultAssetSignatureValidator(),
            new DefaultTrustResolver(),
            new Rfc3161TimestampProvider("https://freetsa.org/tsr", f.CreateLogger<Rfc3161TimestampProvider>())
        );
    }

    [Test]
    public void SetWithNoCertificates_ReturnsUnsigned()
    {
        var result = _releaseValidator.ValidateReleaseFile(
            new SignedAssetFeed([]) { Assets = [new(null, null, new() { FileName = "test-asset.nupkg", SHA256 = "123" })] }
        );
        result.Assets.First().ValidationResult.Code.ShouldBe(ValidationResultCode.Unsigned);
    }

    [Test]
    public void MissingSha256_ReturnsUnsigned()
    {
        ECDsa ecdsa = ECDsa.Create();
        var result = _releaseValidator.ValidateReleaseFile(
            new SignedAssetFeed([])
            {
                Assets = [new(null, null, new() { FileName = "test-asset.nupkg", SHA256 = null })],
                Certificates = [s_certString]
            }
        );
        result.Assets.First().ValidationResult.Code.ShouldBe(ValidationResultCode.Unsigned);
    }

    [Test]
    public void InvalidSignatureq_ReturnsUnsigned()
    {
        ECDsa ecdsa = ECDsa.Create();
        var result = _releaseValidator.ValidateReleaseFile(
            new SignedAssetFeed([])
            {
                Assets = [new(null, null, new() { FileName = "test-asset.nupkg", SHA256 = null })],
                Certificates = [s_certString]
            }
        );
        result.Assets.First().ValidationResult.Code.ShouldBe(ValidationResultCode.Unsigned);
    }
}

public class NUnitLogger : ILoggerProvider, ILogger, ILoggerFactory
{
    public static readonly NUnitLogger Instance = new();
    
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return this;
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        TestContext.Out.WriteLine(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull
    {
        return this;
    }
}