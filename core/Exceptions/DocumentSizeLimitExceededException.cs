namespace Scv.Core.Exceptions;

/// <summary>
/// Indicates that a document exceeded a configured conversion or merge resource limit.
/// </summary>
public class DocumentSizeLimitExceededException : Exception
{
    public DocumentSizeLimitExceededException(string message) : base(message) { }

    public DocumentSizeLimitExceededException(string message, Exception innerException)
        : base(message, innerException) { }
}
