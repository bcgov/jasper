using CsvHelper;
using CsvHelper.Configuration;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Scv.Api.Documents.Parsers;

public class CsvParser : ICsvParser
{
    public IEnumerable<T> Parse<T>(MemoryStream csvStream, string delimeter = "\t") where T : class
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimeter
        };

        using var reader = new StreamReader(csvStream);
        using var csv = new CsvReader(reader, config);
        var records = csv.GetRecords<T>().ToList();

        return records;
    }
}
