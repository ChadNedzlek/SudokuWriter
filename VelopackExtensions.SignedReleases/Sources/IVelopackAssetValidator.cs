using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Sources;

public interface IVelopackAssetValidator
{
    AssetValidationResult Validate(ValidatedAsset asset);
}

public static class VelopackAssetValidator
{
    public static AssetValidationResult Validate(this IVelopackAssetValidator assetValidator, UpdateInfo updateInfo)
    {
        AssetValidationResult minValidation = null;
        foreach (var item in updateInfo.DeltasToTarget)
        {
            AssetValidationResult res = assetValidator.Validate(item);
            if (minValidation == null)
            {
                minValidation = res;
            }
            else if (minValidation.Code > res.Code)
            {
                minValidation = res;
            }
        }

        var fullValidation = assetValidator.Validate(updateInfo.TargetFullRelease);
        if (minValidation == null)
        {
            minValidation = fullValidation;
        }
        else if (minValidation.Code > fullValidation.Code)
        {
            minValidation = fullValidation;
        }

        return minValidation;
    }

    public static AssetValidationResult Validate(this IVelopackAssetValidator assetValidator, VelopackAsset asset)
        => asset switch
        {
            ValidatedAsset validated => assetValidator.Validate(validated),
            _ => UnsignedValidationResult.Instance
        };
}