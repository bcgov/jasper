using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;

namespace Scv.Api.Documents.Parsers;

public class CsvParser : ICsvParser
{
    public IEnumerable<T> Parse<T>(MemoryStream csvStream, char delimeter = '\t') where T : class
    {
        var options = new CsvReaderOptions
        {
            Separator = delimeter
        };

        using var reader = new StreamReader(csvStream);
        using var csv = CsvReader.Create(reader, options);
        var records = csv.GetRecords<T>().ToList();

        return records;
    }
}
