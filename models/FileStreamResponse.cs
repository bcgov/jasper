namespace Scv.Models
{
    /// <summary>
    /// Represents a file stream response with metadata for file downloads.
    /// </summary>
    public sealed record FileStreamResponse(
        Stream Stream,
        string FileName,
        string ContentType
    )
    {
        /// <summary>
        /// Gets the size of the stream in bytes, if available.
        /// Returns -1 if the stream does not support seeking or length.
        /// </summary>
        public long SizeBytes => Stream?.CanSeek == true ? Stream.Length : -1;
    }
}
