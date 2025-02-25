using AutoMapper;
using Scv.Api.Models.Location;
using JC = JCCommon.Clients.LocationServices;
using PCSS = PCSSCommon.Models;

namespace Scv.Api.Mappers
{
    public class LocationProfile : Profile
    {
        public LocationProfile()
        {
            // JC mappings
            CreateMap<JC.CodeValue, Location>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.ShortDesc))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LongDesc))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.Flex == "Y"));

            CreateMap<JC.CodeValue, CourtRoom>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.Flex))
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.ShortDesc));

            // PCSS
            CreateMap<PCSS.CourtRoom, CourtRoom>()
                .ForMember(dest => dest.Room, opt => opt.MapFrom(src => src.CourtRoomCd));

            CreateMap<PCSS.Location, Location>()
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.Code, opt => opt.MapFrom(src => src.JustinAgenId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.LocationSNm))
                .ForMember(dest => dest.Active, opt => opt.MapFrom(src => src.ActiveYn == "Y"));
        }
    }
}
