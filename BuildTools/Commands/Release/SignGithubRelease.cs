using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Octokit;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Signing;

namespace VaettirNet.BuildTools.Commands.Release;

public partial class SignGithubRelease : CommandBase
{
    private readonly ReleaseSignerFactory _signerFactory;
    private readonly IFeedSerializer _serializer;
    public string Repository { get; private set; }
    public string CertificateFilePath { get; private set; }
    public string PrivateKeyFilePath { get; private set; }
    public string AccessToken { get; private set; }
    public string ReleaseTag { get; private set; }

    public SignGithubRelease(ReleaseSignerFactory signerFactory, IFeedSerializer serializer) : base("gh release sign", "Sign the asset files in a github release")
    {
        _signerFactory = signerFactory;
        _serializer = serializer;
        Options = new()
        {
            {"repository|repo|r=", "Repository to sign releases of", v => Repository = v},
            {"release-tag|tag=", "Release tag (optional, default to latest)", v => ReleaseTag = v},
            {"private-key|key|k=", "Private key (or pfx) file.", v => PrivateKeyFilePath = v},
            {"certificate|cert|c=", "Certificate file (optional if pfx is used for key)", v => CertificateFilePath = v},
            {"access-token|token|t=", "Access token to github release", v => AccessToken = v},
        };
    }
    
    protected override int Execute()
    {
        return ExecuteAsync().GetAwaiter().GetResult();

        async Task<int> ExecuteAsync()
        {
            ValidateRequiredArgument(Repository, "repository");
            ValidateRequiredArgument(PrivateKeyFilePath, "private-key");

            if (string.IsNullOrEmpty(AccessToken))
            {
                AccessToken = Environment.GetEnvironmentVariable("GITHUB_ACCESSTOKEN");
            }
        
            ValidateRequiredArgument(AccessToken, "access-token");
        
            X509Certificate2 cert = null;
            SigningProcessor signingProcessor;
            if (Path.GetExtension(PrivateKeyFilePath).Equals(".pfx", StringComparison.InvariantCultureIgnoreCase))
            {
                cert = X509CertificateLoader.LoadPkcs12FromFile(PrivateKeyFilePath, null, X509KeyStorageFlags.EphemeralKeySet);
                signingProcessor = SigningProcessor.FromCertificate(cert);
            }
            else
            {
                ReadOnlySpan<char> keyText = (await File.ReadAllTextAsync(PrivateKeyFilePath)).AsSpan();
                signingProcessor = SigningProcessor.FromPem(keyText);
            }

            if (cert == null)
            {
                ValidateRequiredArgument(CertificateFilePath, "certificate");
                cert = X509CertificateLoader.LoadCertificate(await File.ReadAllBytesAsync(CertificateFilePath));
            }

            ReleaseSigner signer = _signerFactory.Create(cert, signingProcessor);

            Connection connection =
                new(new ProductHeaderValue("VaettirNet.BuildTool", Assembly.GetExecutingAssembly().GetName().Version.ToString()));
            if (!string.IsNullOrEmpty(AccessToken))
            {
                connection.Credentials = new Credentials(AccessToken);
            }

            string[] repoParts = Repository.Split('/', 2);
            if (repoParts.Length != 2)
            {
                CommandSet.WriteError("Invalid repository, should be in the format 'owner/name'");
                Options.WriteOptionDescriptions(CommandSet.Error);
                return 1;
            }

            string owner = repoParts[0];
            string repo = repoParts[1];
        
            ApiConnection api = new(connection);
            ReleasesClient releasesClient = new(api);
            Octokit.Release targetRelease;
            try
            {
                if (string.IsNullOrEmpty(ReleaseTag))
                {
                    targetRelease = await releasesClient.GetLatest(owner, repo);
                }
                else
                {
                    targetRelease = await releasesClient.Get(owner, repo, ReleaseTag);
                }
            }
            catch (ApiException e) when (e.StatusCode == HttpStatusCode.NotFound)
            {
                CommandSet.WriteError("Unable to find specified release");
                return 3;
            }

            if (!targetRelease.Assets.Any(a => ReleaseFileNameRegex().IsMatch(a.Name)))
            {
                CommandSet.WriteError("Target release does not contain a release.*.json file");
                return 3;
            }

            Octokit.Release previousRelease = null;
            IReadOnlyList<Octokit.Release> firstPage = await releasesClient.GetAll(owner, repo, new ApiOptions { PageSize = 5, PageCount = 1 });
            bool foundCurrent = false;
            foreach (Octokit.Release searchRelease in firstPage.OrderByDescending(r => r.CreatedAt))
            {
                if (foundCurrent)
                {
                    previousRelease = searchRelease;
                    break;
                }

                foundCurrent = searchRelease.Id == targetRelease.Id;
            }

            Dictionary<string, string> previousSignatures = [];
            Dictionary<string, string> previousCertHash = [];
            Dictionary<string, string> previousCertificates = [];
            HttpClient client = new HttpClient();
            
            if (previousRelease is null)
            {
                CommandSet.Write("Unable to find previous release, signing all assets");
            }
            else
            {
                CommandSet.WriteVerbose("Found previous release tagged {0}", previousRelease.TagName);
                foreach (ReleaseAsset releaseAsset in targetRelease.Assets)
                {
                    if (!ReleaseFileNameRegex().IsMatch(releaseAsset.Name)) continue;

                    SignedAssetFeed signedAssetFeed;
                    await using (Stream stream = await client.GetStreamAsync(releaseAsset.BrowserDownloadUrl))
                    {
                        signedAssetFeed = _serializer.Deserialize<SignedAssetFeed>(stream);
                    }

                    foreach (string certBase64 in signedAssetFeed.Certificates ?? [])
                    {
                        using X509Certificate2 releaseCert = CertificateUtility.ReadCertificateFromBase64(certBase64, out string thumbprint);
                        previousCertificates.TryAdd(thumbprint, certBase64);
                    }

                    foreach (SignedAsset asset in signedAssetFeed.Assets ?? [])
                    {
                        previousSignatures.TryAdd(asset.SHA256, asset.SignatureBase64);
                        previousCertHash.TryAdd(asset.SHA256, asset.CertHash);
                    }
                }
            }

            foreach (ReleaseAsset releaseAsset in targetRelease.Assets)
            {
                if (!ReleaseFileNameRegex().IsMatch(releaseAsset.Name)) continue;

                CommandSet.WriteVerbose("Downloading previous release asset {0}", releaseAsset.Name);
                
                SignedAssetFeed signedAssetFeed;
                await using (Stream stream = await client.GetStreamAsync(releaseAsset.BrowserDownloadUrl))
                {
                    signedAssetFeed = _serializer.Deserialize<SignedAssetFeed>(stream);
                }

                HashSet<string> addedCertHash = [];
                List<string> certText = [];
                
                foreach (string certBase64 in signedAssetFeed.Certificates ?? [])
                {
                    using X509Certificate2 releaseCert = CertificateUtility.ReadCertificateFromBase64(certBase64, out string thumbprint);
                    if (addedCertHash.Add(thumbprint))
                    {
                        certText.Add(certBase64);
                    }
                }
                
                foreach (SignedAsset asset in signedAssetFeed.Assets ?? [])
                {
                    if (string.IsNullOrEmpty(asset.SignatureBase64))
                    {
                        if (!string.IsNullOrEmpty(asset.SHA256) && !string.IsNullOrEmpty(asset.CertHash))
                        {
                            if (!addedCertHash.Contains(asset.CertHash))
                            {
                                CommandSet.WriteWarning(
                                    "Certificate not found for asset {0}, removing signature (hash: {1})",
                                    asset.FileName,
                                    asset.CertHash
                                );
                                asset.SignatureBase64 = null;
                                asset.CertHash = null;
                            }
                        }
                        else
                        {
                            if (previousSignatures.TryGetValue(asset.SHA256, out string sign) &&
                                previousCertHash.TryGetValue(asset.SHA256, out string hash) &&
                                previousCertificates.TryGetValue(hash, out string certBase64))
                            {
                                if (addedCertHash.Add(hash))
                                {
                                    certText.Add(certBase64);
                                }

                                CommandSet.WriteVerbose("Asset {0} was found in previous release, reusing signature", asset.FileName);
                                asset.SignatureBase64 = sign;
                                asset.CertHash = hash;
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(asset.SHA256) || string.IsNullOrEmpty(asset.CertHash))
                        {
                            CommandSet.WriteWarning("Detected signature with no hashes for asset {0}, removing signature", asset.FileName);
                            asset.SignatureBase64 = null;
                            asset.CertHash = null;
                        }
                        else if (!addedCertHash.Contains(asset.CertHash))
                        {
                            CommandSet.WriteWarning(
                                "Certificate not found for asset {0}, removing signature (hash: {1})",
                                asset.FileName,
                                asset.CertHash
                            );
                            asset.SignatureBase64 = null;
                            asset.CertHash = null;
                        }
                        else
                        {
                            CommandSet.WriteVerbose("Asset {0} is already signed by cert {1}, leaving", asset.FileName, asset.CertHash);
                        }
                    }
                }

                signedAssetFeed = signedAssetFeed with { Certificates = certText.ToImmutableList() };

                signedAssetFeed = signer.SignRelease(signedAssetFeed);

                CommandSet.WriteVerbose("Deleting previous release asset {0}", releaseAsset.Name);
                await releasesClient.DeleteAsset(owner, repo, releaseAsset.Id);

                using var uploadStream = new MemoryStream();
                _serializer.Serialize(uploadStream, signedAssetFeed);
                uploadStream.Seek(0, SeekOrigin.Begin);

                CommandSet.Write("Re-uploading release asset {0}", releaseAsset.Name);
                ReleaseAsset uploaded = await releasesClient.UploadAsset(
                    targetRelease,
                    new ReleaseAssetUpload(releaseAsset.Name, releaseAsset.ContentType, uploadStream, null)
                );
                if (!string.IsNullOrEmpty(releaseAsset.Label))
                {
                    CommandSet.WriteVerbose("Relabelling previous release asset {0}", releaseAsset.Name);
                    await releasesClient.EditAsset(owner, repo, uploaded.Id, new ReleaseAssetUpdate(uploaded.Name) { Label = releaseAsset.Label });
                }
            }
            
            CommandSet.Write("Done.");
            return 0;
        }
    }

    [GeneratedRegex(@"^releases\..*\.json$")]
    private static partial Regex ReleaseFileNameRegex();
}