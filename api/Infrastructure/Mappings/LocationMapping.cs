using Mapster;
using Scv.Models.Location;
using System.Collections.Generic;
using JC = JCCommon.Clients.LocationServices;
using PCSS = PCSSCommon.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class LocationMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<JC.CodeValue, Location>()
           .ConstructUsing(src => Location.Create(
               src.LongDesc,
               src.ShortDesc,
               src.Code,
               src.Flex == "Y",
               new List<CourtRoom>()))
           .Map(dest => dest.Code, src => src.ShortDesc)
           .Map(dest => dest.LocationId, src => src.Code)
           .Ignore(dest => dest.InfoLink);

        config.NewConfig<JC.CodeValue, CourtRoom>()
            .Map(dest => dest.LocationId, src => src.Flex)
            .Map(dest => dest.Room, src => src.Code)
            .Map(dest => dest.Type, src => src.ShortDesc);

        config.NewConfig<PCSS.CourtRoom, CourtRoom>()
            .Map(dest => dest.Room, src => src.CourtRoomCd);

        config.NewConfig<PCSS.Location, Location>()
            .ConstructUsing(src => Location.Create(
                src.LocationSNm,
                src.JustinAgenId != null ? src.JustinAgenId.ToString() : null,
                src.LocationId != null ? src.LocationId.ToString() : null,
                src.ActiveYn == "Y",
                new List<CourtRoom>()
                ))
            .Ignore(dest => dest.InfoLink)
            .IgnoreIf((src, dest) => src.CourtRooms == null, dest => dest.CourtRooms);
    }
}
