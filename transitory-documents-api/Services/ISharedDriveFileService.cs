using Scv.Models.TransitoryDocuments;
using Scv.TdApi.Models;

namespace Scv.TdApi.Services
{
    public interface ISharedDriveFileService
    {
        /// <summary>
        /// Finds files in the shared drive based on region code, agency identifier, room code, and date.
        /// </summary>
        /// <returns>List of file metadata</returns>
        Task<IReadOnlyList<FileMetadataDto>> FindFilesAsync(
            TransitoryDocumentSearchRequest request);

        Task<Scv.Models.FileStreamResponse> OpenFileAsync(string relativePath);
    }
}
