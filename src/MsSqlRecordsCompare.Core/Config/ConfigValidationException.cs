namespace MsSqlRecordsCompare.Core.Config;

public class ConfigValidationException : Exception
{
    public ConfigValidationException(string message) : base(message) { }
    public ConfigValidationException(string message, Exception innerException) : base(message, innerException) { }
}
