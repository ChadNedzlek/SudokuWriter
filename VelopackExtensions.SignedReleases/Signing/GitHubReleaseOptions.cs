namespace VaettirNet.VelopackExtensions.SignedReleases.Signing;

public class GitHubReleaseOptions
{
    public string RepoUrl { get; set; }
    public string AccessToken { get; set; }
    public bool Prerelease { get; set; }
}