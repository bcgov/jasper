using System;
using System.Collections.Generic;
using Mapster;
using PCSSCommon.Models;
using Scv.Db.Models;
using Scv.Models;
using Scv.Models.Document;

namespace Scv.Api.Infrastructure.Mappings;

public class DocumentCategoryMapping : IRegister
{
    // ROPs and PSRs are acronyms so we don't want to change their casing
    private static readonly HashSet<string> CategoriesToSkip = new(
    [
        DocumentCategories.ROP,
        DocumentCategories.PSR
    ], StringComparer.OrdinalIgnoreCase);

    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PcssConfiguration, DocumentCategoryDto>()
            .Map(dest => dest.Name, src => TransformName(src.Key));

        config.NewConfig<PcssConfiguration, DocumentCategory>()
            .Map(dest => dest.Name, src => TransformName(src.Key))
            .Map(dest => dest.ExternalId, src => src.PcssConfigurationId);

        config.NewConfig<DocumentCategory, DocumentCategory>()
            .Ignore(dest => dest.Id);
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
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

        return char.ToUpper(value[0]) + value[1..].ToLower();
    }
}
