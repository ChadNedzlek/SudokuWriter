using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using VaettirNet.VelopackExtensions.SignedReleases.Model;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public interface IFeedSerializer
{
    T Deserialize<T>(Stream stream);
    void Serialize<T>(Stream stream, T value);
    T Deserialize<T>(ReadOnlySpan<byte> data);
}

public class DefaultFeedSerializer : IFeedSerializer
{
    public static readonly DefaultFeedSerializer Basic = new(BuildBasicOptions());
    
    private readonly JsonSerializerOptions _options;

    public static JsonSerializerOptions BuildBasicOptions()
    {
        JsonSerializerOptions o = new();
        UpdateBasicOptions(o);
        return o;
    }

    public static void UpdateBasicOptions(JsonSerializerOptions o)
    {
        o.WriteIndented = false;
        o.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault;
        o.Converters.Add(new SemanticVersionConverter());
        o.Converters.Add(new JsonStringEnumConverter());
    }

    public DefaultFeedSerializer(IOptionsSnapshot<JsonSerializerOptions> options)
    {
        _options = options.Get(OptionNames.VelopackAssetsOptions);
    }
    
    public DefaultFeedSerializer(JsonSerializerOptions options)
    {
        _options = options;
    }

    public T Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, _options);
    public void Serialize<T>(Stream stream, T value) => JsonSerializer.Serialize(stream, value, _options);
    public T Deserialize<T>(ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, _options);
}