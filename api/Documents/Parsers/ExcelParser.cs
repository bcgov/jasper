using System.IO;

namespace Scv.Api.Documents.Parsers;

public class ExcelParser : IExcelParser
{
    public IExcelWorkbook Open(Stream excelStream)
        => new ExcelWorkbook(excelStream);
}
