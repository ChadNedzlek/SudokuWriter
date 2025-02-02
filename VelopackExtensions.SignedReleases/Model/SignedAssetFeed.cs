using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Velopack;

namespace VaettirNet.VelopackExtensions.SignedReleases.Model;

public record SignedAssetFeed(ImmutableList<string> Certificates) : VelopackAssetFeed
{
    public ImmutableList<string> Certificates { get; set; } = Certificates;
    
    public new IEnumerable<SignedAsset> Assets
    {
        get => base.Assets.Cast<SignedAsset>();
        init => base.Assets = value.Cast<VelopackAsset>().ToArray();
    }
    
    public string TimeStamp { get; set; }
}