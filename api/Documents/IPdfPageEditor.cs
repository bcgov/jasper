using System.Collections.Generic;
using System.IO;

namespace Scv.Api.Documents;

public interface IPdfPageEditor
{
    MemoryStream AppendPages(MemoryStream document, MemoryStream pages);
    MemoryStream FillFormFields(MemoryStream document, IReadOnlyDictionary<string, string> fieldValues, bool flatten);
    MemoryStream RemoveLastPage(MemoryStream document);
}
