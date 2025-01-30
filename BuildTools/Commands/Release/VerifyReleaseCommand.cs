using System;
using System.IO;
using Mono.Options;
using VaettirNet.VelopackExtensions.SignedReleases;
using VaettirNet.VelopackExtensions.SignedReleases.Model.Validation;

namespace VaettirNet.BuildTools.Commands.Release;

public class VerifyReleaseCommand : CommandBase
{
    private readonly ReleaseValidator _validator;
    public string ReleaseFilePath { get; private set; }

    public VerifyReleaseCommand(ReleaseValidator validator) : base("vpk release verify", "Verifies the signatures of files in a velopack release.json or directory")
    {
        _validator = validator;
        Options = new OptionSet { { "release|rel|r=", "Velopack release.json file path", v => ReleaseFilePath = v }, };
    }

    protected override int Execute()
    {
        ValidateRequiredArgument(ReleaseFilePath, "release");
        if (Directory.Exists(ReleaseFilePath))
        {
            int ret = 0;
            foreach(var file in Directory.GetFiles(ReleaseFilePath, "releases.*.json", SearchOption.TopDirectoryOnly))
            {
                CommandSet.Out.WriteLine($"=== {Path.GetFileName(file)}");
                ret |= ValidateSingleFile(file);
            }
            return 0;
        }

        return ValidateSingleFile(ReleaseFilePath);
    }

    private int ValidateSingleFile(string releaseFilePath)
    {
        var feed = _validator.ValidateReleaseFile(releaseFilePath);
        int ret = 0;
        foreach (var item in feed.Assets)
        {
            switch (item.ValidationResult)
            {
                case UnsignedValidationResult:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    CommandSet.Error.WriteLine($"{item.FileName} | Missing signature | Hash = {item.SHA256}");
                    ret |= 4;
                    break;
                case InvalidSignatureValidationResult res:
                    Console.ForegroundColor = ConsoleColor.Red;
                    CommandSet.Error.WriteLine($"{item.FileName} | Invalid signature | Hash = {item.SHA256} | Signature = {res.Signature}");
                    ret |= 8;
                    break;
                case UnverifiableValidationResult res:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    CommandSet.Error.WriteLine($"{item.FileName} | Unverifiable signature | Hash = {item.SHA256} | CertHash = {res.CertHash}");
                    ret |= 16;
                    break;
                case VerificationFailedValidationResult res:
                    Console.ForegroundColor = ConsoleColor.Red;
                    CommandSet.Error.WriteLine($"{item.FileName} | Verification failed | Hash = {item.SHA256} | CertHash = {res.Signature} | Signer = {res.Signer.Subject}");
                    ret |= 32;
                    break;
                case UntrustedValidationResult res:
                    CommandSet.Error.WriteLine($"{item.FileName} | Verified signature | Hash = {item.SHA256} | Signer = {res.Signer.Subject}");
                    break;
                case TrustedValidationResult res:
                    Console.ForegroundColor = ConsoleColor.Green;
                    CommandSet.Error.WriteLine($"{item.FileName} | trusted | Hash = {item.SHA256} | Signer = {res.Signer.Subject}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            Console.ResetColor();
        }

        return ret;
    }
}