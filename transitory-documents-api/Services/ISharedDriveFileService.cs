using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Scv.Models;
using Scv.TdApi.Models;

namespace Scv.TdApi.Services
{
    public interface ISharedDriveFileService
    {
        IReadOnlyList<FileMetadataDto> FindFilesAsync(
            string region,
            string location,
            string roomCd,
            DateOnly date);

        Task<Scv.Models.FileStreamResponse> OpenFileAsync(string absolutePath);
    }
}
