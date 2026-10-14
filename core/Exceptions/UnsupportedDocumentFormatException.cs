namespace Scv.Core.Exceptions;

/// <summary>
/// Indicates that a document's declared format is unsupported or does not match its content.
/// </summary>
public class UnsupportedDocumentFormatException : Exception
{
    public UnsupportedDocumentFormatException(string message) : base(message) { }

    public UnsupportedDocumentFormatException(string message, Exception innerException)
        : base(message, innerException) { }
}
