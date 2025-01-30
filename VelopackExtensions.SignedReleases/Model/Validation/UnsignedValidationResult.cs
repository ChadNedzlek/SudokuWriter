namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public record UnsignedValidationResult() : AssetValidationResult(ValidationResultCode.Unsigned)
{
    public static readonly UnsignedValidationResult Instance = new();
}