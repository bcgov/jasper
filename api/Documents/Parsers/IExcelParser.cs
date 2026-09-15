using System.IO;

namespace Scv.Api.Documents.Parsers;

public interface IExcelParser
{
    IExcelWorkbook Open(MemoryStream excelStream);
}
