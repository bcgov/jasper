using Mapster;
using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class DarsMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Lognotes, DarsSearchResults>()
            .Map(dest => dest.Date, src => src.DateTime)
            .Map(dest => dest.LocationId, src => src.Location)
            .Map(dest => dest.CourtRoomCd, src => src.Room)
            .Map(dest => dest.Url, src => src.Url)
            .Map(dest => dest.FileName, src => src.FileName)
            .Map(dest => dest.LocationNm, src => src.LocationName);
    }
}