namespace VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

public enum ValidationResultCode
{
    Unsigned = 0,
    Unverifiable,
    InvalidSignature,
    VerificationFailed,
    Expired,
    Untrusted,
    Trusted
}