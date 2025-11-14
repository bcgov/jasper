namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Abstraction for file system operations supporting both local and SMB file systems.
    /// </summary>
    public interface ISmbFileSystemClient : IDisposable
    {
        /// <summary>
        /// Lists all files in the specified directory path and optionally in subdirectories matching a room filter.
        /// The room filter is normalized (leading zeros removed) and matches folder names containing the same normalized number.
        /// For example, roomFilter "009" will match folders "Room 9", "Courtroom 009", "R9" but not "Room 69".
        /// If the path contains a wildcard (*), it will match directories that start with the prefix before the wildcard.
        /// </summary>
        /// <param name="path">Relative path from the base path (may include * wildcard for prefix matching)</param>
        /// <param name="roomFilter">Optional room code to filter subdirectories</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Collection of file information objects</returns>
        Task<IReadOnlyList<SmbFileInfo>> ListFilesAsync(
            string path,
            string? roomFilter = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Opens a file for reading and returns its contents as a memory stream.
        /// </summary>
        /// <param name="filePath">Relative path to the file from the base path</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Memory stream containing the file contents</returns>
        Task<Stream> OpenFileAsync(string filePath, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Represents file information from the file system.
    /// </summary>
    public sealed class SmbFileInfo
    {
        public required string FileName { get; init; }
        public required string FullPath { get; init; }
        public required string Extension { get; init; }
        public required long SizeBytes { get; init; }
        public required DateTime CreatedUtc { get; init; }
        public required string? RelativeDirectory { get; init; }
    }
}