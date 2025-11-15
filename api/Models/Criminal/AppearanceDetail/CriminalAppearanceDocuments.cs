using System.Collections.Generic;
using Scv.Api.Helpers.Documents;
using Scv.Api.Models.Criminal.Detail;

namespace Scv.Api.Models.Criminal.AppearanceDetail;

public class CriminalAppearanceDocuments
{
    public IEnumerable<CriminalDocument> Documents { get; set; }
    public IEnumerable<CriminalDocument> KeyDocuments => KeyDocumentResolver.GetCriminalKeyDocuments(Documents);
}