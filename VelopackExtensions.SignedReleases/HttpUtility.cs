using System;

namespace VaettirNet.VelopackExtensions.SignedReleases.Sources;

public static class HttpUtility
{
    public static Uri WithQueryParameter(this Uri uri, string key, string value)
    {
        UriBuilder b = new UriBuilder(uri);
        string query = b.Query;
        if (string.IsNullOrEmpty(query))
        {
            b.Query = Uri.EscapeDataString(key) + '=' + Uri.EscapeDataString(value);
        }
        else
        {
            b.Query = query + '&' + Uri.EscapeDataString(key) + '=' + Uri.EscapeDataString(value);
        }

        return b.Uri;
    }
}