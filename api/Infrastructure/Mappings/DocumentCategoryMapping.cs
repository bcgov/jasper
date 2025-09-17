using System;
using System.Collections.Generic;
using Mapster;
using PCSSCommon.Models;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class DocumentCategoryMapping : IRegister
{
    // ROPs and PSRs are acronyms so we don't want to change their casing
    private static readonly HashSet<string> CategoriesToSkip = new(
    [
        DocumentCategory.ROP,
        DocumentCategory.PSR
    ], StringComparer.OrdinalIgnoreCase);

    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PcssConfiguration, DocumentCategoryDto>()
            .Map(dest => dest.Name, src => TransformName(src.Key));

        config.NewConfig<PcssConfiguration, DocumentCategory>()
            .Map(dest => dest.Name, src => TransformName(src.Key))
            .Map(dest => dest.ExternalId, src => src.PcssConfigurationId);

        config.NewConfig<DocumentCategory, DocumentCategory>()
            .Ignore(dest => dest.Id);
    }

    /// <summary>
    /// Capitalize the first letter and make the rest lowercase unless it's in the skip list
    /// </summary>
    /// <param name="value">Category value </param>
    /// <returns>Transformed value</returns>
    private static string TransformName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || CategoriesToSkip.Contains(value))
        {
            return value ?? string.Empty;
        }

        return char.ToUpper(value[0]) + value.Substring(1).ToLower();
    }
}
