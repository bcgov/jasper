namespace Scv.Core.Exceptions;

/// <summary>
/// Indicates that a supported document is malformed, encrypted, unconvertible, or would lose meaningful content.
/// </summary>
public class UnprocessableDocumentException : Exception
{
    public UnprocessableDocumentException(string message) : base(message) { }

    public UnprocessableDocumentException(string message, Exception innerException)
        : base(message, innerException) { }
}
