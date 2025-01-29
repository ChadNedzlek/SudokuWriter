namespace VaettirNet.BuildTools;

public class MissingRequiredArgumentException : CommandFailedException
{
    public MissingRequiredArgumentException(string argumentName) : base($"Missing required parameter '{argumentName}'", 1)
    {
    }
}