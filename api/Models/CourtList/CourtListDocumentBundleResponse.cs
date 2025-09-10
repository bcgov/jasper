using System.Collections.Generic;
using Scv.Api.Models.Document;

namespace Scv.Api.Models.CourtList;

public class CourtListDocumentBundleResponse
{
    public List<BinderDto> Binders { get; set; }
    public PdfDocumentResponse PdfResponse { get; set; }
}
