using System;
using System.IO;

namespace JCCommon.Helpers;

/// <summary>
/// Provides functionality to download files from an Elastic File System (EFS). This class is designed to handle large files and avoid the API Gateway limits.
/// </summary>
/// <remarks>
/// Consider putting this helper class in a shared library if it is needed across multiple projects (e.g. pcss-client)
/// </remarks>
public static class FileDownloader
{
    /// <summary>
    /// Downloads a file from EFS and returns it as a stream.
    /// The file will be automatically deleted when the stream is closed.
    /// </summary>
    /// <param name="efsFilePath">The full path to the file in EFS</param>
    /// <returns>A stream containing the file contents that will delete the file on disposal</returns>
    public static Stream DownloadDocument(string efsFilePath)
    {
        if (string.IsNullOrWhiteSpace(efsFilePath))
        {
            throw new ArgumentException("EFS file path cannot be null or empty.", nameof(efsFilePath));
        }

        if (!File.Exists(efsFilePath))
        {
            throw new FileNotFoundException($"The file was not found at the specified EFS path: {efsFilePath}", efsFilePath);
        }

        // FileOptions.DeleteOnClose will automatically delete the file when the stream is closed
        return new FileStream(
             efsFilePath,
             FileMode.Open,
             FileAccess.Read,
             FileShare.None,
             bufferSize: 4096,
             FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }
}
