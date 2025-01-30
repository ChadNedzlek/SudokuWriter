using System.Collections.Generic;
using System.Linq;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model;

public record ValidatedAssetFeed : VelopackAssetFeed
{
    public new IEnumerable<ValidatedAsset> Assets
    {
        get => base.Assets.Cast<ValidatedAsset>();
        init => base.Assets = value.Cast<VelopackAsset>().ToArray();
    } 
}