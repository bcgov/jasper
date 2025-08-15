using System;
using System.IO;
using System.Threading.Tasks;
using Scv.Api.Models.Document;
using Scv.Api.Services.Files;

namespace Scv.Api.Documents.Strategies;

public class ROPStrategy(FilesService filesService) : IDocumentStrategy
{
    private readonly CriminalFilesService _criminalFilesService = filesService.Criminal;
    
    public DocumentType Type => DocumentType.ROP;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var courtLevelCd = Enum.Parse<JCCommon.Clients.FileServices.CourtLevelCd>(documentRequest.CourtLevelCd, true);
        var courtClassCd = Enum.Parse<JCCommon.Clients.FileServices.CourtClassCd>(documentRequest.CourtClassCd, true);
        var recordsOfProceeding = await _criminalFilesService.RecordOfProceedingsAsync(documentRequest.PartId, documentRequest.ProfSeqNo, courtLevelCd, courtClassCd);
        var bytes = Convert.FromBase64String(recordsOfProceeding.B64Content);
        
        return new MemoryStream(bytes);
    }
}