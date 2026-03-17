using System.Collections.Generic;
using Scv.Models.Criminal.Detail;
using Scv.Models.Helpers;

namespace Scv.Api.Models.Criminal.AppearanceDetail;

public class CriminalAppearanceDocuments
{
    public IEnumerable<CriminalDocument> Documents { get; set; }
    public IEnumerable<CriminalDocument> KeyDocuments => KeyDocumentResolver.GetCriminalKeyDocuments(Documents);
}