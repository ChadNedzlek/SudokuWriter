using System.Linq;
using VaettirNet.VelopackExtensions.SignedReleases.Model;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases;

public interface IVelopackAssetValidator
{
    AssetValidationResult Validate(ValidatedAsset asset);
}

public static class VelopackAssetValidator
{
    public static AssetValidationResult Validate(this IVelopackAssetValidator assetValidator, UpdateInfo updateInfo)
    {
        return updateInfo.DeltasToTarget.Append(updateInfo.TargetFullRelease).Select(assetValidator.Validate).MinBy(v => v.Code);
    }

    public static AssetValidationResult Validate(this IVelopackAssetValidator assetValidator, VelopackAsset asset)
        => asset switch
        {
            ValidatedAsset validated => assetValidator.Validate(validated),
            _ => UnsignedValidationResult.Instance
        };
}