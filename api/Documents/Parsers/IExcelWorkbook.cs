using System;
using System.Collections.Generic;

namespace Scv.Api.Documents.Parsers;

public interface IExcelWorkbook : IDisposable
{
    IReadOnlyList<T> GetSheet<T>(string sheetName) where T : class;
}
