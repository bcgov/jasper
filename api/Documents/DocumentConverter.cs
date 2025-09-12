using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using MapsterMapper;
using Scv.Api.Models.Criminal.Detail;
using Scv.Api.Services;

namespace Scv.Api.Documents;

public class DocumentConverter(IMapper mapper, LookupService lookupService) : IDocumentConverter
{
    private readonly IMapper _mapper = mapper;
    private readonly LookupService _lookupService = lookupService;

    public async Task<ICollection<CriminalDocument>> GetCriminalDocuments(CfcAccusedFile ac)
    {
        var criminalDocuments = _mapper.Map<List<CriminalDocument>>(ac.Document);

        //Create ROPs.
        if (ac.Appearance != null && ac.Appearance.Count != 0)
        {
            criminalDocuments.Insert(0, new CriminalDocument
            {
                DocumentTypeDescription = "Record of Proceedings",
                DocmFormDsc = "Record of Proceedings",
                ImageId = ac.PartId,
                Category = "rop",
                PartId = ac.PartId,
                HasFutureAppearance = ac.Appearance?.Any(a =>
                    a?.AppearanceDate != null && DateTime.Parse(a.AppearanceDate) >= DateTime.Today)
            });
        }

        //Populate extra fields.
        foreach (var document in criminalDocuments)
        {
            document.Category = string.IsNullOrEmpty(document.Category)
                ? await _lookupService.GetDocumentCategory(document.DocmFormId, document.DocmClassification)
                : document.Category;
            document.DocumentTypeDescription = document.DocmFormDsc;
            document.PartId = string.IsNullOrEmpty(ac.PartId) ? null : ac.PartId;
            document.DocmId = string.IsNullOrEmpty(document.DocmId) ? null : document.DocmId;
            document.ImageId = string.IsNullOrEmpty(document.ImageId) ? null : document.ImageId;
            document.HasFutureAppearance = ac.Appearance?.Any(a =>
                a?.AppearanceDate != null && DateTime.Parse(a.AppearanceDate) >= DateTime.Today);
        }
        return criminalDocuments;
    }
}
