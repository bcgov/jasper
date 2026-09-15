using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Scv.Api.Documents.Parsers;

public class ExcelWorkbook : IExcelWorkbook
{
    private readonly SpreadsheetDocument _document;
    private readonly WorkbookPart _workbookPart;
    private readonly string[] _sharedStrings;

    public ExcelWorkbook(MemoryStream excelStream)
    {
        _document = SpreadsheetDocument.Open(excelStream, false);
        _workbookPart = _document.WorkbookPart
            ?? throw new InvalidOperationException("Unable to open the Excel workbook.");

        if (_workbookPart.Workbook.Sheets == null || !_workbookPart.Workbook.Sheets.Any())
        {
            throw new InvalidOperationException("The spreadsheet contains no sheets.");
        }

        _sharedStrings = _workbookPart.SharedStringTablePart?.SharedStringTable
            ?.Elements<SharedStringItem>()
            .Select(x => x.InnerText)
            .ToArray() ?? [];
    }

    public IReadOnlyList<T> GetSheet<T>(string sheetName) where T : class
    {
        var sheet = _workbookPart.Workbook.Descendants<Sheet>()
            .FirstOrDefault(s => s.Name == sheetName)
            ?? throw new ArgumentException($"Sheet '{sheetName}' not found.", nameof(sheetName));

        var worksheetPart = (WorksheetPart)_workbookPart.GetPartById(sheet.Id!);
        var rows = worksheetPart.Worksheet.GetFirstChild<SheetData>()?.Elements<Row>().ToList() ?? [];

        var results = new List<T>();
        if (rows.Count == 0)
        {
            return results;
        }

        var columnMap = BuildColumnMap<T>(rows[0]);

        foreach (var row in rows.Skip(1))
        {
            var item = Activator.CreateInstance<T>();
            foreach (var cell in row.Elements<Cell>())
            {
                var column = GetColumnLetters(cell.CellReference?.Value);
                if (column.Length == 0 || !columnMap.TryGetValue(column, out var property))
                {
                    continue;
                }

                property.SetValue(item, ConvertValue(GetCellValue(cell), property.PropertyType));
            }

            results.Add(item);
        }

        return results;
    }

    public void Dispose() => _document.Dispose();

    private Dictionary<string, PropertyInfo> BuildColumnMap<T>(Row headerRow) where T : class
    {
        var propertiesByHeader = typeof(T)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToDictionary(
                p => p.GetCustomAttribute<ExcelColumnAttribute>()?.Name ?? p.Name,
                p => p,
                StringComparer.OrdinalIgnoreCase);

        var columnMap = new Dictionary<string, PropertyInfo>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.Elements<Cell>())
        {
            var header = GetCellValue(cell).Trim();
            var column = GetColumnLetters(cell.CellReference?.Value);
            if (header.Length == 0 || column.Length == 0)
            {
                continue;
            }

            if (propertiesByHeader.TryGetValue(header, out var property))
            {
                columnMap[column] = property;
            }
        }

        return columnMap;
    }

    private string GetCellValue(Cell cell)
    {
        var value = cell.CellValue?.InnerText ?? cell.InnerText ?? string.Empty;

        if (cell.DataType?.Value == CellValues.SharedString)
        {
            return int.TryParse(value, out var index) && index >= 0 && index < _sharedStrings.Length
                ? _sharedStrings[index]
                : string.Empty;
        }

        if (cell.DataType?.Value == CellValues.Boolean)
        {
            return value == "1" ? "true" : "false";
        }

        return value;
    }

    private static string GetColumnLetters(string cellReference)
    {
        if (string.IsNullOrEmpty(cellReference))
        {
            return string.Empty;
        }

        var length = 0;
        while (length < cellReference.Length && char.IsLetter(cellReference[length]))
        {
            length++;
        }

        return cellReference[..length];
    }

    private static object? ConvertValue(string raw, Type targetType)
    {
        var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;
        var isNullable = Nullable.GetUnderlyingType(targetType) != null || !targetType.IsValueType;

        if (string.IsNullOrEmpty(raw))
        {
            return isNullable ? null : Activator.CreateInstance(underlyingType);
        }

        if (underlyingType == typeof(string))
        {
            return raw;
        }

        if (underlyingType.IsEnum)
        {
            return Enum.Parse(underlyingType, raw, ignoreCase: true);
        }

        if (underlyingType == typeof(DateTime))
        {
            return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial)
                ? DateTime.FromOADate(serial)
                : DateTime.Parse(raw, CultureInfo.InvariantCulture);
        }

        return Convert.ChangeType(raw, underlyingType, CultureInfo.InvariantCulture);
    }
}
