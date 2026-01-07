using Mapster;
using MapsterMapper;
using Scv.Api.Infrastructure.Mappings;
using Xunit;
using CourtRoom = Scv.Models.Location.CourtRoom;
using JC = JCCommon.Clients.LocationServices;
using Location = Scv.Models.Location.Location;
using PCSS = PCSSCommon.Models;

namespace tests.api.Infrastructure.Mappings;

public class LocationMappingTests
{
    private readonly IMapper _mapper;

    public LocationMappingTests()
    {
        var config = new TypeAdapterConfig();
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);
    }

    [Fact]
    public void JC_CodeValue_To_Location_Mapping_ShouldBeValid()
    {
        var source = new JC.CodeValue
        {
            LongDesc = "Long Description",
            ShortDesc = "Short Description",
            Code = "123",
            Flex = "Y"
        };

        var result = _mapper.Map<Location>(source);

        Assert.Equal("Long Description", result.Name);
        Assert.Equal("Short Description", result.Code);
        Assert.Equal("123", result.LocationId);
        Assert.True(result.Active);
        Assert.Empty(result.CourtRooms);
    }

    [Fact]
    public void JC_CodeValue_To_CourtRoom_Mapping_ShouldBeValid()
    {
        var source = new JC.CodeValue
        {
            Flex = "LocationId",
            Code = "RoomCode",
            ShortDesc = "RoomType"
        };

        var result = _mapper.Map<CourtRoom>(source);

        Assert.Equal("LocationId", result.LocationId);
        Assert.Equal("RoomCode", result.Room);
        Assert.Equal("RoomType", result.Type);
    }

    [Fact]
    public void PCSS_CourtRoom_To_CourtRoom_Mapping_ShouldBeValid()
    {
        var source = new PCSS.CourtRoom
        {
            CourtRoomCd = "RoomCode"
        };

        var result = _mapper.Map<CourtRoom>(source);

        Assert.Equal("RoomCode", result.Room);
    }

    [Fact]
    public void PCSS_Location_To_Location_Mapping_ShouldBeValid()
    {
        var source = new PCSS.Location
        {
            LocationSNm = "Location Name",
            JustinAgenId = 123,
            LocationId = 456,
            ActiveYn = "Y"
        };

        var result = _mapper.Map<Location>(source);

        Assert.Equal("Location Name", result.Name);
        Assert.Equal("123", result.Code);
        Assert.Equal("456", result.LocationId);
        Assert.True(result.Active);
        Assert.Empty(result.CourtRooms);
    }

    [Fact]
    public void PCSS_Location_To_Location_Mapping_ShouldHandleNullValues()
    {
        var source = new PCSS.Location
        {
            LocationSNm = "Location Name",
            JustinAgenId = null,
            LocationId = null,
            ActiveYn = "N"
        };

        var result = _mapper.Map<Location>(source);

        Assert.Equal("Location Name", result.Name);
        Assert.Null(result.Code);
        Assert.Null(result.LocationId);
        Assert.False(result.Active);
        Assert.Empty(result.CourtRooms);
    }
}