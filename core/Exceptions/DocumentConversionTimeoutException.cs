namespace Scv.Core.Exceptions;

/// <summary>
/// Indicates that document conversion exceeded the application-enforced deadline.
/// </summary>
public class DocumentConversionTimeoutException : Exception
{
    public DocumentConversionTimeoutException(string message) : base(message) { }

    public DocumentConversionTimeoutException(string message, Exception innerException)
        : base(message, innerException) { }
}
