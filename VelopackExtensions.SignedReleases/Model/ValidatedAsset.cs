using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model;

public record ValidatedAsset : VelopackAsset
{
    public ValidatedAsset(AssetValidationResult validationResult, VelopackAsset baseAsset)
    {
        ValidationResult = validationResult;
        
        baseAsset.CopyTo(this);
    }

    public AssetValidationResult ValidationResult { get; init; }
}