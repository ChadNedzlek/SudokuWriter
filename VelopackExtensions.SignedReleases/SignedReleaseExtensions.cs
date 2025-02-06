using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Services;

// services.AddStuff() is, by convention, put in this namespace, no matter the assembly
// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class SignedReleaseExtensions
{
    public static IServiceCollection AddVelopackReleaseValidation(this IServiceCollection services, string timestampAuthorityUrl = "https://freetsa.org/tsr")
    {
        services.AddLogging();
        services.AddOptions();
        services.Configure<JsonSerializerOptions>(
            OptionNames.VelopackAssetsOptions,
            o =>
            {
                o.WriteIndented = false;
                o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
                o.Converters.Add(new SemanticVersionConverter());
                o.Converters.Add(new JsonStringEnumConverter());
            }
        );
        services.TryAddSingleton<IFeedSerializer, DefaultFeedSerializer>();
        services.TryAddSingleton<IAssetSignatureValidator>(DefaultAssetSignatureValidator.Instance);
        services.TryAddSingleton<IAssetTrustResolver>(DefaultTrustResolver.Instance);
        services.TryAddSingleton<ReleaseSignerFactory>();
        services.TryAddSingleton<ReleaseValidator>();
        services.TryAddSingleton<ISignedTimestampService, Rfc3161TimestampProvider>();
        services.Configure<Rfc3161TimestampProvider.Options>(o => o.TimestampAuthorityUrl = timestampAuthorityUrl);
        return services;
    }
}