using System.IO;

namespace Scv.Api.Documents;

public interface IPdfPageEditor
{
    MemoryStream AppendPages(MemoryStream document, MemoryStream pages);
    MemoryStream RemoveLastPage(MemoryStream document);
}
