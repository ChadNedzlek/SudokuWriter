using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public static class VelopackUtilities
{
    public static void CopyTo(this VelopackAsset source, VelopackAsset dest)
    {
        dest.PackageId = source.PackageId;
        dest.Version = source.Version;
        dest.NotesMarkdown = source.NotesMarkdown;
        dest.NotesHTML = source.NotesHTML;
        dest.Size = source.Size;
        dest.SHA1 = source.SHA1;
        dest.SHA256 = source.SHA256;
        dest.FileName = source.FileName;
        dest.Type = source.Type;
    }

    public static string GetVeloReleaseIndexName(string channel)
    {
        return $"releases.{channel ?? VelopackRuntimeInfo.SystemOs.GetOsShortName()}.json";
    }
}