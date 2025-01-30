using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public class SameSignerAsEntryPointValidator : IVelopackAssetValidator
{
    private readonly ILogger<SameSignerAsEntryPointValidator> _logger;
    private readonly string _subjectName;
    private readonly string _subjectKeyId;

    public SameSignerAsEntryPointValidator(ILogger<SameSignerAsEntryPointValidator> logger = null)
    {
        _logger = logger;
        try
        {
#pragma warning disable SYSLIB0057
            X509Certificate2 cert = new X509Certificate2(Assembly.GetEntryAssembly().Location);
#pragma warning restore SYSLIB0057
            _subjectName = cert.Subject;
            _subjectKeyId = cert.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault()?.SubjectKeyIdentifier;
            _logger?.LogInformation("Entry point is signed with subject='{subjectName}' and subjectKeyIdentifier='{subjectKeyIdentifier}'", _subjectName, _subjectKeyId);
        }
        catch
        {
            _logger?.LogInformation("Entry point is unsigned, any valid signature will be trusted");
        }
    }

    public AssetValidationResult Validate(ValidatedAsset asset)
    {
        switch (asset.ValidationResult)
        {
            case UntrustedValidationResult untrusted:
                return CheckSigner(untrusted.Signer);

            default:
                return asset.ValidationResult;
        }

        AssetValidationResult CheckSigner(X509Certificate2 signer)
        {
            if (!string.IsNullOrEmpty(_subjectName) && signer.Subject != _subjectName)
            {
                _logger?.LogWarning(
                    "Asset '{assetName}' subject name '{assetSubjectName}' does not match expected '{expectedSubjectName}', untrusted.",
                    asset.FileName,
                    signer.Subject,
                    _subjectName
                );
                return new UntrustedValidationResult(signer);
            }

            if (!string.IsNullOrEmpty(_subjectKeyId))
            {
                string signerKeyId = signer.Extensions.OfType<X509SubjectKeyIdentifierExtension>().FirstOrDefault()?.SubjectKeyIdentifier;
                if (_subjectKeyId != signerKeyId)
                {
                    _logger?.LogWarning(
                        "Asset '{assetName}' subject key id '{assetSubjectKeyId}' does not match expected '{expectedSubjectKeyId}', untrusted.",
                        asset.FileName,
                        signerKeyId,
                        _subjectKeyId
                    );
                    return new UntrustedValidationResult(signer);
                }

                _logger?.LogDebug("Asset '{assetName}' matches subject and key id", asset.FileName);
                return new TrustedValidationResult(signer);
            }

            return new TrustedValidationResult(signer);
        }
    }
}