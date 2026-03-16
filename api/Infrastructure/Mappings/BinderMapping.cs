using System.Collections.Generic;
using JCCommon.Clients.FileServices;
using Mapster;
using Scv.Models.Criminal.Detail;
using Scv.Db.Models;
using Scv.Models;
using System;
using Scv.Models.Civil.Detail;
using Scv.Models.Civil.Detail;

namespace Scv.Api.Infrastructure.Mappings;

public class BinderMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<BinderDto, Binder>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Upd_Dtm);

        config.NewConfig<Binder, BinderDto>()
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm);

        config.NewConfig<BinderDocumentDto, BinderDocument>()
            .Map(dest => dest.DocumentType, src => (int)src.DocumentType);

        config.NewConfig<BinderDocument, BinderDocumentDto>()
            .Map(dest => dest.DocumentType, src => (DocumentType)src.DocumentType);

        config.NewConfig<CriminalDocument, BinderDocumentDto>()
            .Map(dest => dest.DocumentId, src =>
                string.IsNullOrWhiteSpace(src.ImageId)
                    ? null
                    : src.ImageId)
            .Map(dest => dest.FileName, src => src.DocumentTypeDescription)
            .Map(dest => dest.DocumentType, src =>
                string.Equals(src.Category, DocumentCategory.ROP, StringComparison.OrdinalIgnoreCase)
                    ? DocumentType.ROP
                    : DocumentType.File)
            .Map(dest => dest.Category, src => src.Category)
            .Map(dest => dest.FiledDt, src => src.IssueDate);

        config.NewConfig<CivilDocument, BinderDocumentDto>()
            .Map(dest => dest.DocumentId, src => src.CivilDocumentId)
            .Map(dest => dest.FileName, src => src.DocumentTypeDescription)
            .Map(dest => dest.DocumentType, src =>
                string.Equals(src.DocumentTypeCd, DocumentCategory.CSR, StringComparison.OrdinalIgnoreCase)
                    ? DocumentType.CourtSummary
                    : DocumentType.File)
            .Map(dest => dest.Category, src => src.DocumentTypeCd)
            .Map(dest => dest.Issues, src => src.Issue != null ? src.Issue.Adapt<List<IssueDto>>() : new List<IssueDto>())
            .Map(dest => dest.FiledBy, src => src.FiledBy != null ? src.FiledBy.Adapt<List<FiledByDto>>() : new List<FiledByDto>());

        config.NewConfig<CvfcIssue, IssueDto>();
        config.NewConfig<CivilIssue, IssueDto>();
        config.NewConfig<ClFiledBy, FiledByDto>();
        config.NewConfig<CvfcDocumentSupport, DocumentSupportDto>();
    }
}
