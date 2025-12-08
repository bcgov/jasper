using System;
using System.Collections.Generic;
using System.Linq;
using Bogus;
using Scv.Api.Helpers.Extensions;
using Xunit;

namespace tests.api.Helpers.Extensions;

public class EnumerableExtensionsTests
{
    private readonly Faker _faker;

    public EnumerableExtensionsTests()
    {
        _faker = new Faker();
    }

    private class TestEntity
    {
        public string Id { get; set; }
        public string DateString { get; set; }
        public string Name { get; set; }
    }

    private List<TestEntity> GenerateTestEntities(int count, Func<int, string> dateGenerator = null)
    {
        dateGenerator ??= (i => _faker.Date.Future().ToString("dd-MMM-yyyy"));

        return [.. Enumerable.Range(0, count)
            .Select(i => new TestEntity
            {
                Id = _faker.Random.AlphaNumeric(10),
                DateString = dateGenerator(i),
                Name = _faker.Name.FullName()
            })];
    }

    #region Basic Sorting Tests

    [Theory]
    [InlineData(false)] // Ascending
    [InlineData(true)]  // Descending
    public void OrderByDateString_WithValidDates_SortsCorrectly(bool descending)
    {
        var items = new List<TestEntity>
        {
            new() { Id = "1", DateString = "25-Oct-2025" },
            new() { Id = "2", DateString = "20-Oct-2025" },
            new() { Id = "3", DateString = "30-Oct-2025" }
        };

        var result = items.OrderByDateString(x => x.DateString, descending).ToList();

        var expectedOrder = descending
            ? ["3", "1", "2"] // 30, 25, 20
            : new[] { "2", "1", "3" }; // 20, 25, 30

        Assert.Equal(expectedOrder, result.Select(x => x.Id));
    }

    [Fact]
    public void OrderByDateString_WithMultipleDates_SortsInChronologicalOrder()
    {
        var dates = new[]
        {
            _faker.Date.Past(1).ToString("dd-MMM-yyyy"),
            _faker.Date.Future(1).ToString("dd-MMM-yyyy"),
            _faker.Date.Recent(30).ToString("dd-MMM-yyyy"),
            _faker.Date.Soon(30).ToString("dd-MMM-yyyy")
        }.OrderBy(x => _faker.Random.Int()).ToList(); // Shuffle

        var items = dates.Select((date, i) => new TestEntity
        {
            Id = $"Item-{i}",
            DateString = date
        }).ToList();

        var result = items.OrderByDateString(x => x.DateString).ToList();

        for (int i = 1; i < result.Count; i++)
        {
            var prev = DateTime.Parse(result[i - 1].DateString);
            var current = DateTime.Parse(result[i].DateString);
            Assert.True(prev <= current, $"Items not in chronological order: {prev} should be <= {current}");
        }
    }

    #endregion

    #region Null and Empty Handling Tests

    [Theory]
    [InlineData(true)]  // Nulls last
    [InlineData(false)] // Nulls first
    public void OrderByDateString_WithNullDates_PlacesNullsCorrectly(bool nullsLast)
    {
        var validDate = _faker.Date.Future().ToString("dd-MMM-yyyy");
        var items = new List<TestEntity>
        {
            new() { Id = "valid", DateString = validDate },
            new() { Id = "null", DateString = null },
            new() { Id = "empty", DateString = "" },
            new() { Id = "whitespace", DateString = "   " }
        };

        var result = items.OrderByDateString(x => x.DateString, nullsLast: nullsLast).ToList();

        var nullIds = new[] { "null", "empty", "whitespace" };
        var nullPositions = result
            .Select((item, index) => new { item.Id, Index = index })
            .Where(x => nullIds.Contains(x.Id))
            .Select(x => x.Index)
            .ToList();

        if (nullsLast)
        {
            Assert.All(nullPositions, pos => Assert.True(pos > 0, "Nulls should be at the end"));
        }
        else
        {
            Assert.All(nullPositions, pos => Assert.True(pos < result.Count - 1, "Nulls should be at the beginning"));
        }
    }

    [Fact]
    public void OrderByDateString_WithAllNullDates_MaintainsOriginalOrder()
    {
        var items = GenerateTestEntities(5, i => null);
        var originalIds = items.Select(x => x.Id).ToList();

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Equal(originalIds, result.Select(x => x.Id));
    }

    #endregion

    #region Invalid Date Handling Tests

    [Fact]
    public void OrderByDateString_WithInvalidDates_PlacesInvalidLast()
    {
        var validDate = _faker.Date.Future().ToString("dd-MMM-yyyy");
        var items = new List<TestEntity>
        {
            new() { Id = "valid", DateString = validDate },
            new() { Id = "invalid1", DateString = _faker.Random.String2(10) },
            new() { Id = "invalid2", DateString = "NOT-A-DATE" }
        };

        var result = items.OrderByDateString(x => x.DateString, nullsLast: true).ToList();

        Assert.Equal("valid", result[0].Id);
        Assert.Contains(result[1].Id, new[] { "invalid1", "invalid2" });
        Assert.Contains(result[2].Id, new[] { "invalid1", "invalid2" });
    }

    [Fact]
    public void OrderByDateString_WithMixedValidAndInvalidDates_SeparatesCorrectly()
    {
        var items = new List<TestEntity>
        {
            new() { Id = "1", DateString = "25-Oct-2025" },
            new() { Id = "2", DateString = null },
            new() { Id = "3", DateString = "20-Oct-2025" },
            new() { Id = "4", DateString = _faker.Lorem.Word() }, // Invalid
            new() { Id = "5", DateString = "15-Nov-2025" }
        };

        var result = items.OrderByDateString(x => x.DateString).ToList();

        var validDateIds = result.Take(3).Select(x => x.Id).ToList();
        var invalidDateIds = result.Skip(3).Select(x => x.Id).ToList();

        Assert.Contains("3", validDateIds); // 20-Oct
        Assert.Contains("1", validDateIds); // 25-Oct
        Assert.Contains("5", validDateIds); // 15-Nov
        Assert.Contains("2", invalidDateIds); // null
        Assert.Contains("4", invalidDateIds); // invalid
    }

    #endregion

    #region Date Format Tests

    [Fact]
    public void OrderByDateString_WithPCSSFormat_ParsesCorrectly()
    {
        var items = new List<TestEntity>
        {
            new() { Id = "1", DateString = "25-Oct-2025" },
            new() { Id = "2", DateString = "20-Jan-2025" },
            new() { Id = "3", DateString = "30-Dec-2025" }
        };

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Equal("2", result[0].Id); // January
        Assert.Equal("1", result[1].Id); // October
        Assert.Equal("3", result[2].Id); // December
    }

    [Fact]
    public void OrderByDateString_WithAlternativeFormats_ParsesCorrectly()
    {
        var items = new List<TestEntity>
        {
            new() { Id = "iso", DateString = "2025-10-25" },
            new() { Id = "pcss", DateString = "25-Oct-2025" },
            new() { Id = "iso2", DateString = "2025-10-20" }
        };

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Contains(result[0].Id, new[] { "iso2" }); // 2025-10-20
        Assert.Contains(result[1].Id, new[] { "iso", "pcss" }); // 2025-10-25 (same date)
    }

    [Theory]
    [InlineData(2024, 2025, 2026)]
    [InlineData(2023, 2024, 2025)]
    public void OrderByDateString_WithDifferentYears_SortsCorrectly(int year1, int year2, int year3)
    {
        var items = new List<TestEntity>
        {
            new() { Id = "3", DateString = $"25-Oct-{year3}" },
            new() { Id = "1", DateString = $"25-Oct-{year1}" },
            new() { Id = "2", DateString = $"25-Oct-{year2}" }
        };

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Equal("1", result[0].Id);
        Assert.Equal("2", result[1].Id);
        Assert.Equal("3", result[2].Id);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void OrderByDateString_WithEmptyCollection_ReturnsEmpty()
    {
        var items = new List<TestEntity>();

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void OrderByDateString_WithSingleItem_ReturnsSingleItem()
    {
        var items = GenerateTestEntities(1);

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Single(result);
        Assert.Equal(items[0].Id, result[0].Id);
    }

    [Fact]
    public void OrderByDateString_WithLargeDataset_PerformsSorting()
    {
        var items = GenerateTestEntities(_faker.Random.Int(100, 500));

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Equal(items.Count, result.Count);

        var validDates = result
            .Where(x => DateTime.TryParse(x.DateString, out _))
            .ToList();

        for (int i = 1; i < validDates.Count; i++)
        {
            var prev = DateTime.Parse(validDates[i - 1].DateString);
            var current = DateTime.Parse(validDates[i].DateString);
            Assert.True(prev <= current);
        }
    }

    #endregion

    #region Exception Tests

    [Fact]
    public void OrderByDateString_WithNullSource_ThrowsArgumentNullException()
    {
        List<TestEntity> items = null;

        Assert.Throws<ArgumentNullException>(() =>
            items.OrderByDateString(x => x.DateString));
    }

    [Fact]
    public void OrderByDateString_WithNullDateSelector_ThrowsArgumentNullException()
    {
        var items = GenerateTestEntities(3);

        Assert.Throws<ArgumentNullException>(() =>
            items.OrderByDateString(null));
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void OrderByDateString_WithChainedLinqOperations_WorksCorrectly()
    {
        var items = GenerateTestEntities(10);

        var result = items
            .Where(x => !string.IsNullOrEmpty(x.DateString))
            .OrderByDateString(x => x.DateString)
            .Take(5)
            .ToList();

        Assert.True(result.Count <= 5);

        // Verify chronological order
        for (int i = 1; i < result.Count; i++)
        {
            var prev = DateTime.Parse(result[i - 1].DateString);
            var current = DateTime.Parse(result[i].DateString);
            Assert.True(prev <= current);
        }
    }

    [Fact]
    public void OrderByDateString_WithRealWorldScenario_SortsCorrectly()
    {
        var items = new List<TestEntity>
        {
            new() { Id = "RJ-1", DateString = "15-Jan-2025" },
            new() { Id = "RJ-2", DateString = "20-Dec-2024" },
            new() { Id = "RJ-3", DateString = "" }, // No due date
            new() { Id = "DEC-1", DateString = "10-Feb-2025" },
            new() { Id = "DEC-2", DateString = "05-Feb-2025" },
            new() { Id = "CNT-1", DateString = "25-Jan-2025" }
        };

        var result = items.OrderByDateString(x => x.DateString).ToList();

        Assert.Equal("RJ-2", result[0].Id);   // 20-Dec-2024
        Assert.Equal("RJ-1", result[1].Id);   // 15-Jan-2025
        Assert.Equal("CNT-1", result[2].Id);  // 25-Jan-2025
        Assert.Equal("DEC-2", result[3].Id);  // 05-Feb-2025
        Assert.Equal("DEC-1", result[4].Id);  // 10-Feb-2025
        Assert.Equal("RJ-3", result[5].Id);   // empty (last)
    }

    #endregion
}