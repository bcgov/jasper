using System;
using Mapster;
using Scv.Api.Documents;
using Scv.Api.Models;
using Scv.Api.Models.Criminal.Detail;
using Scv.Db.Models;

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
                    : DocumentType.File);
    }
}
