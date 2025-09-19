using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;

namespace Scv.Api.Documents.Parsers;

public class CsvParser : ICsvParser
{
    public IEnumerable<T> Parse<T>(MemoryStream csvStream) where T : class
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = "\t"
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<T>().ToList();

        return records;
    }
}
