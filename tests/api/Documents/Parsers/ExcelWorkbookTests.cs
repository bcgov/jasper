using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Scv.Api.Documents.Parsers;
using Scv.Api.Documents.Parsers.Models;
using Xunit;

namespace tests.api.Documents.Parsers;

public class ExcelWorkbookTests
{
    private const string COURT_LOCATIONS_SHEET = "Court Locations";
    private const string ADULT_PROBATION_OFFICES_SHEET = "Adult Probation Offices";

    private static readonly string[] CourtLocationHeaders =
    [
        "Location Name", "Location Code (4 digit)", "JASPER Name", "Location Path",
        "Court Address 1", "Court Address 2", "Location Staffed",
        "JCM Phone 1", "JCM Phone 1 Notes", "Email 1", "Email 1 Notes",
        "IAR Schedule", "FXD Schedule", "Adult Probation Office", "Youth Probation Office"
    ];

    private static readonly string[] VictoriaRow =
    [
        "Victoria Law Courts", "3581", "Victoria", "BC/Vancouver Island/Victoria",
        "850 Burdett Ave", "Victoria, BC V8W 1B4", "Yes",
        "250-356-1478", "Main line", "victoria.jcm@gov.bc.ca", "General inquiries",
        "Mon-Fri", "Tue/Thu", "Victoria Probation", "Victoria Youth Probation"
    ];

    private static readonly string[] NanaimoRow =
    [
        "Nanaimo Law Courts", "3561", "Nanaimo", "BC/Vancouver Island/Nanaimo",
        "35 Front St", "Nanaimo, BC V9R 5J1", "Yes",
        "250-741-5860", "", "nanaimo.jcm@gov.bc.ca", "",
        "Mon-Fri", "Wed", "Nanaimo Probation", "Nanaimo Youth Probation"
    ];

    #region Constructor

    [Fact]
    public void Constructor_Throws_When_Stream_Is_Not_Spreadsheet()
    {
        using var stream = new MemoryStream([1, 2, 3, 4]);

        Assert.ThrowsAny<Exception>(() => new ExcelWorkbook(stream));
    }

    [Fact]
    public void Constructor_Throws_When_Workbook_Has_No_Sheets()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
        }
        stream.Position = 0;

        var ex = Assert.Throws<InvalidOperationException>(() => new ExcelWorkbook(stream));
        Assert.Equal("The spreadsheet contains no sheets.", ex.Message);
    }

    [Fact]
    public void Constructor_Succeeds_Without_SharedStringTable()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, useSharedStrings: false, CourtLocationHeaders, VictoriaRow);

        using var wb = new ExcelWorkbook(stream);
        var rows = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);

        Assert.Single(rows);
        Assert.Equal("Victoria Law Courts", rows[0].Name);
    }

    #endregion

    #region GetSheet - sheet lookup

    [Fact]
    public void GetSheet_Throws_When_Sheet_Not_Found()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, VictoriaRow);
        using var wb = new ExcelWorkbook(stream);

        var ex = Assert.Throws<ArgumentException>(() => wb.GetSheet<CourtLocation>("Youth Probation Offices"));
        Assert.Contains("Youth Probation Offices", ex.Message);
    }

    [Fact]
    public void GetSheet_Returns_Empty_When_Sheet_Has_No_Rows()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, header: null);
        using var wb = new ExcelWorkbook(stream);

        var rows = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetSheet_Returns_Empty_When_Only_Header_Row_Exists()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders);
        using var wb = new ExcelWorkbook(stream);

        var rows = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetSheet_Can_Read_Multiple_Sheets_From_Same_Workbook()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, COURT_LOCATIONS_SHEET, CourtLocationHeaders, [VictoriaRow, NanaimoRow]);
            AddSheet(wbPart, ADULT_PROBATION_OFFICES_SHEET,
                ["Office Name", "Address", "Phone 1", "Toll Free"],
                [["Victoria Probation", "836 Courtney St", "250-387-3232", "1-800-555-0100"]]);
        }
        stream.Position = 0;

        using var wb = new ExcelWorkbook(stream);
        var locations = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);
        var offices = wb.GetSheet<AdultProbationOffice>(ADULT_PROBATION_OFFICES_SHEET);

        Assert.Equal(["Victoria Law Courts", "Nanaimo Law Courts"], locations.Select(x => x.Name));
        var office = Assert.Single(offices);
        Assert.Equal("Victoria Probation", office.Name);
        Assert.Equal("836 Courtney St", office.Address1);
        Assert.Equal("250-387-3232", office.Phone1);
        Assert.Equal("1-800-555-0100", office.TollFree);
    }

    #endregion

    #region GetSheet - header mapping

    [Fact]
    public void GetSheet_Maps_All_CourtLocation_Columns()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, VictoriaRow);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
        Assert.Equal("3581", cl.Code);
        Assert.Equal("Victoria", cl.JasperName);
        Assert.Equal("BC/Vancouver Island/Victoria", cl.Path);
        Assert.Equal("850 Burdett Ave", cl.Address1);
        Assert.Equal("Victoria, BC V8W 1B4", cl.Address2);
        Assert.Equal("Yes", cl.Staffed);
        Assert.Equal("250-356-1478", cl.JcmPhone1);
        Assert.Equal("Main line", cl.JcmPhone1Notes);
        Assert.Equal("victoria.jcm@gov.bc.ca", cl.Email1);
        Assert.Equal("General inquiries", cl.Email1Notes);
        Assert.Equal("Mon-Fri", cl.IarSchedule);
        Assert.Equal("Tue/Thu", cl.FxdSchedule);
        Assert.Equal("Victoria Probation", cl.AdultProbationOffice);
        Assert.Equal("Victoria Youth Probation", cl.YouthProbationOffice);
    }

    [Fact]
    public void GetSheet_Maps_Header_Case_Insensitively()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["LOCATION NAME", "jasper name"], ["Victoria Law Courts", "Victoria"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
        Assert.Equal("Victoria", cl.JasperName);
    }

    [Fact]
    public void GetSheet_Trims_Header_Whitespace()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["  Location Name  "], ["Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
    }

    [Fact]
    public void GetSheet_Does_Not_Map_Property_Name_When_Attribute_Overrides_It()
    {
        // The property is "Name" but the attribute says "Location Name" - a header of "Name" must not match.
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["Name"], ["Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("", cl.Name);
    }

    [Fact]
    public void GetSheet_Ignores_Unknown_Columns()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["Region", "Location Name"], ["Island", "Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
    }

    [Fact]
    public void GetSheet_Ignores_Empty_Header_Cells()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["", "Location Name"], ["ignored", "Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
    }

    [Fact]
    public void GetSheet_Maps_Columns_By_Header_Not_Position()
    {
        // Columns in a different order than the model's property declaration.
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true,
            ["JASPER Name", "Location Code (4 digit)", "Location Name"],
            ["Victoria", "3581", "Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
        Assert.Equal("3581", cl.Code);
        Assert.Equal("Victoria", cl.JasperName);
    }

    [Fact]
    public void GetSheet_Leaves_Default_When_Column_Is_Absent_From_Sheet()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, ["Location Name"], ["Victoria Law Courts"]);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("", cl.Code);
        Assert.Equal("", cl.AdultProbationOffice);
    }

    #endregion

    #region GetSheet - values

    [Fact]
    public void GetSheet_Returns_Rows_In_Sheet_Order()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, VictoriaRow, NanaimoRow);
        using var wb = new ExcelWorkbook(stream);

        var rows = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);

        Assert.Equal(["3581", "3561"], rows.Select(x => x.Code));
    }

    [Fact]
    public void GetSheet_Sets_Null_For_Empty_String_Cells()
    {
        // Documents current behaviour: an empty cell overrides the model's "" default with null.
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, NanaimoRow);
        using var wb = new ExcelWorkbook(stream);

        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Null(cl.JcmPhone1Notes);
        Assert.Null(cl.Email1Notes);
    }

    [Fact]
    public void GetSheet_Leaves_Default_When_Cell_Is_Missing_From_Row()
    {
        // Row 2 only has column B populated; column A (Location Name) is absent from the row's XML entirely.
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, COURT_LOCATIONS_SHEET, ["Location Name", "Location Code (4 digit)"], [], extraRows:
            [
                new Row(new Cell { CellReference = "B2", DataType = CellValues.String, CellValue = new CellValue("3581") }) { RowIndex = 2 },
            ]);
        }
        stream.Position = 0;

        using var wb = new ExcelWorkbook(stream);
        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("", cl.Name);
        Assert.Equal("3581", cl.Code);
    }

    [Fact]
    public void GetSheet_Reads_Inline_String_Cells()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, COURT_LOCATIONS_SHEET, ["Location Name"], [], extraRows:
            [
                new Row(new Cell(new InlineString(new Text("Victoria Law Courts"))) { CellReference = "A2", DataType = CellValues.InlineString }) { RowIndex = 2 },
            ]);
        }
        stream.Position = 0;

        using var wb = new ExcelWorkbook(stream);
        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        Assert.Equal("Victoria Law Courts", cl.Name);
    }

    [Fact]
    public void GetSheet_Returns_Empty_For_Out_Of_Range_SharedString_Index()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, COURT_LOCATIONS_SHEET, ["Location Name"], [], extraRows:
            [
                new Row(new Cell { CellReference = "A2", DataType = CellValues.SharedString, CellValue = new CellValue("999") }) { RowIndex = 2 },
            ]);
        }
        stream.Position = 0;

        using var wb = new ExcelWorkbook(stream);
        var cl = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET).Single();

        // Empty string on a reference type maps to null.
        Assert.Null(cl.Name);
    }

    #endregion

    #region GetSheet - non-string conversions

    private enum Status { Unknown, Open, Closed }

    private class TypedRow
    {
        public int Count { get; set; }
        public int? OptionalCount { get; set; }
        public decimal Amount { get; set; }
        public bool Flag { get; set; }
        public DateTime When { get; set; }
        public DateTime? OptionalWhen { get; set; }
        public Status Status { get; set; }
    }

    [Fact]
    public void GetSheet_Converts_Supported_Value_Types()
    {
        var when = new DateTime(2024, 3, 15);
        using var stream = BuildWorkbook("Typed", true,
            ["Count", "OptionalCount", "Amount", "Flag", "When", "OptionalWhen", "Status"],
            ["30", "88", "1234.56", "true", when.ToOADate().ToString("R"), "", "open"]);
        using var wb = new ExcelWorkbook(stream);

        var row = wb.GetSheet<TypedRow>("Typed").Single();

        Assert.Equal(30, row.Count);
        Assert.Equal(88, row.OptionalCount);
        Assert.Equal(1234.56m, row.Amount);
        Assert.True(row.Flag);
        Assert.Equal(when, row.When);
        Assert.Null(row.OptionalWhen);
        Assert.Equal(Status.Open, row.Status);
    }

    [Fact]
    public void GetSheet_Converts_Boolean_Typed_Cells()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, "Typed", ["Flag"], [], extraRows:
            [
                new Row(new Cell { CellReference = "A2", DataType = CellValues.Boolean, CellValue = new CellValue("1") }) { RowIndex = 2 },
                new Row(new Cell { CellReference = "A3", DataType = CellValues.Boolean, CellValue = new CellValue("0") }) { RowIndex = 3 },
            ]);
        }
        stream.Position = 0;

        using var wb = new ExcelWorkbook(stream);
        var rows = wb.GetSheet<TypedRow>("Typed");

        Assert.True(rows[0].Flag);
        Assert.False(rows[1].Flag);
    }

    [Fact]
    public void GetSheet_Parses_ISO_Date_String()
    {
        using var stream = BuildWorkbook("Typed", true, ["When"], ["2024-03-15"]);
        using var wb = new ExcelWorkbook(stream);

        var row = wb.GetSheet<TypedRow>("Typed").Single();

        Assert.Equal(new DateTime(2024, 3, 15), row.When);
    }

    [Fact]
    public void GetSheet_Uses_Default_For_Empty_Non_Nullable_Value_Types()
    {
        using var stream = BuildWorkbook("Typed", true, ["Count", "Flag", "When", "Status"], ["", "", "", ""]);
        using var wb = new ExcelWorkbook(stream);

        var row = wb.GetSheet<TypedRow>("Typed").Single();

        Assert.Equal(0, row.Count);
        Assert.False(row.Flag);
        Assert.Equal(default, row.When);
        Assert.Equal(Status.Unknown, row.Status);
    }

    [Fact]
    public void GetSheet_Throws_On_Unconvertible_Number()
    {
        using var stream = BuildWorkbook("Typed", true, ["Count"], ["not-a-number"]);
        using var wb = new ExcelWorkbook(stream);

        Assert.Throws<FormatException>(() => wb.GetSheet<TypedRow>("Typed"));
    }

    [Fact]
    public void GetSheet_Throws_On_Invalid_Enum_Value()
    {
        using var stream = BuildWorkbook("Typed", true, ["Status"], ["bogus"]);
        using var wb = new ExcelWorkbook(stream);

        Assert.Throws<ArgumentException>(() => wb.GetSheet<TypedRow>("Typed"));
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_Can_Be_Called_Multiple_Times()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, VictoriaRow);
        var wb = new ExcelWorkbook(stream);

        wb.Dispose();
        var ex = Record.Exception(wb.Dispose);

        Assert.Null(ex);
    }

    [Fact]
    public void Results_Remain_Usable_After_Dispose()
    {
        using var stream = BuildWorkbook(COURT_LOCATIONS_SHEET, true, CourtLocationHeaders, VictoriaRow);
        IReadOnlyList<CourtLocation> rows;

        using (var wb = new ExcelWorkbook(stream))
        {
            rows = wb.GetSheet<CourtLocation>(COURT_LOCATIONS_SHEET);
        }

        Assert.Equal("Victoria Law Courts", rows[0].Name);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Builds a single-sheet workbook. Pass <c>header: null</c> for a sheet with no rows at all.
    /// </summary>
    private static MemoryStream BuildWorkbook(string sheetName, bool useSharedStrings, string[] header, params string[][] dataRows)
    {
        var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var wbPart = doc.AddWorkbookPart();
            wbPart.Workbook = new Workbook(new Sheets());
            AddSheet(wbPart, sheetName, header, dataRows, useSharedStrings);
        }
        stream.Position = 0;
        return stream;
    }

    private static void AddSheet(
        WorkbookPart wbPart,
        string name,
        string[] header,
        string[][] dataRows,
        bool useSharedStrings = true,
        IEnumerable<Row> extraRows = null)
    {
        var wsPart = wbPart.AddNewPart<WorksheetPart>();
        var sheetData = new SheetData();
        wsPart.Worksheet = new Worksheet(sheetData);

        var sheets = wbPart.Workbook.GetFirstChild<Sheets>();
        sheets.Append(new Sheet
        {
            Id = wbPart.GetIdOfPart(wsPart),
            SheetId = (uint)(sheets.Count() + 1),
            Name = name
        });

        uint rowIndex = 1;
        if (header != null)
        {
            sheetData.Append(BuildRow(wbPart, rowIndex++, header, useSharedStrings));
            foreach (var data in dataRows)
            {
                sheetData.Append(BuildRow(wbPart, rowIndex++, data, useSharedStrings));
            }
        }

        if (extraRows != null)
        {
            foreach (var row in extraRows)
            {
                sheetData.Append(row);
            }
        }
    }

    private static Row BuildRow(WorkbookPart wbPart, uint rowIndex, string[] values, bool useSharedStrings)
    {
        var row = new Row { RowIndex = rowIndex };
        for (var i = 0; i < values.Length; i++)
        {
            var reference = $"{ColumnLetter(i)}{rowIndex}";
            row.Append(useSharedStrings
                ? SharedStringCell(wbPart, reference, values[i])
                : new Cell { CellReference = reference, DataType = CellValues.String, CellValue = new CellValue(values[i]) });
        }
        return row;
    }

    private static Cell SharedStringCell(WorkbookPart wbPart, string reference, string text)
    {
        var sstPart = wbPart.SharedStringTablePart ?? wbPart.AddNewPart<SharedStringTablePart>();
        sstPart.SharedStringTable ??= new SharedStringTable();

        var items = sstPart.SharedStringTable.Elements<SharedStringItem>().ToList();
        var index = items.FindIndex(x => x.InnerText == text);
        if (index < 0)
        {
            sstPart.SharedStringTable.AppendChild(new SharedStringItem(new Text(text)));
            index = items.Count;
        }

        return new Cell
        {
            CellReference = reference,
            DataType = CellValues.SharedString,
            CellValue = new CellValue(index.ToString())
        };
    }

    private static string ColumnLetter(int zeroBasedIndex)
    {
        var result = string.Empty;
        var n = zeroBasedIndex + 1;
        while (n > 0)
        {
            n--;
            result = (char)('A' + n % 26) + result;
            n /= 26;
        }
        return result;
    }

    #endregion
}

