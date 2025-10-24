using System;
using Xunit;
using LocationModel = Scv.Models.Location.Location;

namespace tests.api.Models.Location;

public class LocationTest
{
    [Fact]
    public void InfoLink_ShouldReturnCorrectUri_WhenNameIsValid()
    {
        var location = LocationModel.Create("Vancouver Law Courts", default, default, default, default);

        var result = location.InfoLink;

        Assert.Equal(new Uri("https://provincialcourt.bc.ca/court-locations/vancouver"), result);
    }

    [Fact]
    public void InfoLink_ShouldReturnNull_WhenNameIsNull()
    {
        var location = LocationModel.Create(null, default, default, default, default);

        var result = location.InfoLink;

        Assert.Null(result);
    }

    [Fact]
    public void InfoLink_ShouldReturnNull_WhenNameIsEmpty()
    {
        var location = LocationModel.Create(string.Empty, default, default, default, default);

        var result = location.InfoLink;

        Assert.Null(result);
    }

    [Fact]
    public void InfoLink_ShouldFilterOutInvalidWords()
    {
        var location = LocationModel.Create("Court Law British Columbia Provincial Courts", default, default, default, default);

        var result = location.InfoLink;

        Assert.Equal(new Uri("https://provincialcourt.bc.ca/court-locations/british-columbia"), result);
    }

    [Fact]
    public void InfoLink_ShouldHandleMixedCaseWords()
    {
        var location = LocationModel.Create("BRITISH ColUmbia provincial COURT", default, default, default, default);

        var result = location.InfoLink;

        Assert.Equal(new Uri("https://provincialcourt.bc.ca/court-locations/british-columbia"), result);
    }

    [Fact]
    public void Create_ShouldReturnLocation_WhenJcLocationAndPcssLocationAreValid()
    {
        var jcLocation = LocationModel.Create("Vancouver Law Courts", "123", "456", true, default);
        var pcssLocation = LocationModel.Create("Vancouver Law Courts", "123", "789", true, default);

        var result = LocationModel.Create(jcLocation, pcssLocation);

        Assert.NotNull(result);
        Assert.Equal("Vancouver Law Courts", result.Name);
        Assert.Equal("456", result.Code);
        Assert.Equal("789", result.LocationId);
        Assert.Equal(jcLocation.Code, result.AgencyIdentifierCd);
        Assert.True(result.Active);
        Assert.Equal(pcssLocation.CourtRooms, result.CourtRooms);
    }

    [Fact]
    public void Create_ShouldReturnLocation_WhenPcssLocationIsNull()
    {
        var jcLocation = LocationModel.Create("Vancouver Law Courts", "123", "456", true, default);

        var result = LocationModel.Create(jcLocation, null);

        Assert.NotNull(result);
        Assert.Equal("Vancouver Law Courts", result.Name);
        Assert.Equal("456", result.Code);
        Assert.Null(result.LocationId);
        Assert.True(result.Active);
        Assert.Equal(jcLocation.CourtRooms, result.CourtRooms);
    }

    [Fact]
    public void Create_ShouldReturnLocation_WithCorrectShortNameWhenPcssShortNameIsNotAvailable()
    {
        var jcLocation = LocationModel.Create("Vancouver Law Courts", "123", "456", true, default);
        var pcssLocation = LocationModel.Create("Vancouver", "123", "789", true, default);

        var result = LocationModel.Create(jcLocation, pcssLocation);

        Assert.NotNull(result);
        Assert.Equal("Vancouver Law Courts", result.Name);
        Assert.Equal("456", result.Code);
        Assert.NotNull(result.LocationId);
        Assert.True(result.Active);
        Assert.Equal(jcLocation.CourtRooms, result.CourtRooms);
        Assert.Equal("Vancouver", result.ShortName);
    }

    [Fact]
    public void Create_ShouldReturnLocation_WhenPcssLocationHasShortName()
    {
        var jcLocation = LocationModel.Create("Vancouver Law Courts", "123", "456", true, default);
        var pcssLocation = LocationModel.Create("222-Main", "123", "789", true, default);

        var result = LocationModel.Create(jcLocation, pcssLocation);

        Assert.NotNull(result);
        Assert.Equal("Vancouver Law Courts", result.Name);
        Assert.Equal("456", result.Code);
        Assert.NotNull(result.LocationId);
        Assert.True(result.Active);
        Assert.Equal(jcLocation.CourtRooms, result.CourtRooms);
        Assert.Equal(pcssLocation.Name, result.ShortName);
    }
}
