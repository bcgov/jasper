using System.Collections.Generic;
using Scv.Models.Location;
using Xunit;
using LocationModel = Scv.Models.Location.Location;
using LocationsModel = Scv.Models.Location.Locations;

namespace tests.api.Models.Locations;

public class LocationsTest
{
    [Fact]
    public void Add_Location_ShouldIncreaseCount()
    {
        var locations = new LocationsModel();
        var location = LocationModel.Create("TestName", "TestCode", "TestLocationId", true, []);

        locations.Add(location);

        Assert.Single(locations);
    }

    [Fact]
    public void Remove_Location_ShouldDecreaseCount()
    {
        var location = LocationModel.Create("TestName", "TestCode", "TestLocationId", true, new List<CourtRoom>());
        var locations = new LocationsModel([location]);

        var result = locations.Remove(location);

        Assert.True(result);
        Assert.Empty(locations);
    }

    [Fact]
    public void Contains_Location_ShouldReturnTrue()
    {
        var location = LocationModel.Create("TestName", "TestCode", "TestLocationId", true, []);
        var locations = new LocationsModel([location]);

        var result = locations.Contains(location);

        Assert.True(result);
    }

    [Fact]
    public void Indexer_Get_ShouldReturnCorrectLocation()
    {
        var location = LocationModel.Create("TestName", "TestCode", "TestLocationId", true, []);
        var locations = new LocationsModel([location]);

        var result = locations[0];

        Assert.Equal(location, result);
    }

    [Fact]
    public void Indexer_Set_ShouldUpdateLocation()
    {
        var location1 = LocationModel.Create("TestName1", "TestCode1", "TestLocationId1", true, []);
        var location2 = LocationModel.Create("TestName2", "TestCode2", "TestLocationId2", true, []);
        var locations = new LocationsModel([location1])
        {
            [0] = location2
        };

        Assert.Equal(location2, locations[0]);
    }

    [Fact]
    public void Create_ShouldReturnEmptyLocations_WhenPcssLocationsIsEmpty()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel>();

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Empty(result);
    }

    [Fact]
    public void Create_ShouldReturnPcssLocations_WhenJcLocationsIsEmpty()
    {
        var pcssLocation = LocationModel.Create("PCSSName", "PCSSCode", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel>();
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
        Assert.Equal(pcssLocation.Name, result[0].Name);
    }

    [Fact]
    public void Create_ShouldReturnEmptyLocations_WhenBothCollectionsAreEmpty()
    {
        var jcLocations = new List<LocationModel>();
        var pcssLocations = new List<LocationModel>();

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Empty(result);
    }

    [Fact]
    public void Create_ShouldMatchByLocationId_WhenPcssCodeMatchesJcLocationId()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", true, []);
        var pcssLocation = LocationModel.Create("PCSSName", "JCLocationId", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
        Assert.Equal("JCName", result[0].Name);
    }

    [Fact]
    public void Create_ShouldMatchByName_WhenPcssNameMatchesJcCode()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", true, []);
        var pcssLocation = LocationModel.Create("JCCode", "PCSSCode", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
        Assert.Equal("JCName", result[0].Name);
    }

    [Fact]
    public void Create_ShouldReturnPcssLocations_WhenNoMatchFound()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", true, []);
        var pcssLocation = LocationModel.Create("PCSSName", "PCSSCode", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
        Assert.Equal(pcssLocation.Name, result[0].Name);
    }

    [Fact]
    public void Create_ShouldIncludeInactiveLocations()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", false, []);
        var pcssLocation = LocationModel.Create("PCSSName", "JCLocationId", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
    }

    [Fact]
    public void Create_ShouldIncludeLocations_WhenActiveIsNull()
    {
        var jcLocation = LocationModel.Create("JCName", "JCCode", "JCLocationId", null, []);
        var pcssLocation = LocationModel.Create("PCSSName", "JCLocationId", "PCSSLocationId", true, []);
        var jcLocations = new List<LocationModel> { jcLocation };
        var pcssLocations = new List<LocationModel> { pcssLocation };

        var result = LocationsModel.Create(jcLocations, pcssLocations);

        Assert.Single(result);
    }
}
