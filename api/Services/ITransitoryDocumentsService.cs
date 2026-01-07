using Scv.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;
using FileMetadataDto = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Services
{
    /// <summary>
    /// Service for interacting with the Transitory Documents API.
    /// </summary>
    public interface ITransitoryDocumentsService
    {
        /// <summary>
        /// Calls the Transitory Documents API to search for documents.
        /// </summary>
        /// <param name="locationId">The location to retrieve files.</param>
        /// <param name="roomCode">The room within the location.</param>
        /// <param name="date">The date to retrieve files.</param>
        /// <returns>The collection of file metadata from the API.</returns>
        /// <exception cref="ApiException">A server-side error occurred.</exception>
        Task<IEnumerable<FileMetadataDto>> ListSharedDocuments(string locationId, string roomCode, string date);

        /// <summary>
        /// Downloads a file from the Transitory Documents API using the generated client.
        /// </summary>
        /// <param name="path">The relative UNC path to the file (will be normalized to relative path).</param>
        /// <returns>A file stream response containing the stream, file name, and content type.</returns>
        Task<FileStreamResponse> DownloadFile(string path);
    }
}
