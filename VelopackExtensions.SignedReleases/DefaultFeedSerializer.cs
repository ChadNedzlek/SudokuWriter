using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public interface IFeedSerializer
{
    T Deserialize<T>(Stream stream);
    void Serialize<T>(Stream stream, T value);
    T Deserialize<T>(ReadOnlySpan<byte> data);
}

public class DefaultFeedSerializer : IFeedSerializer
{
    private readonly JsonSerializerOptions _options;

    public DefaultFeedSerializer(IOptionsSnapshot<JsonSerializerOptions> options)
    {
        _options = options.Get(OptionNames.VelopackAssetsOptions);
    }

    public T Deserialize<T>(Stream stream) => JsonSerializer.Deserialize<T>(stream, _options);
    public void Serialize<T>(Stream stream, T value) => JsonSerializer.Serialize(stream, value, _options);
    public T Deserialize<T>(ReadOnlySpan<byte> data) => JsonSerializer.Deserialize<T>(data, _options);
}