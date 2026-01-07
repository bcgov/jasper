using Mapster;
using Scv.Models.Timebank;
using PCSS = PCSSCommon.Clients.TimebankServices;

namespace Scv.Api.Infrastructure.Mappings;

public class TimebankMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<PCSS.VacationPayout, VacationPayoutDto>()
            .Map(dest => dest.JudiciaryPersonId, src => src.JudiciaryPersonId)
            .Map(dest => dest.Period, src => src.Period)
            .Map(dest => dest.EffectiveDate, src => src.EffectiveDate)
            .Map(dest => dest.EntitlementCalcType, src => src.EntitlementCalcType)
            .Map(dest => dest.VacationCurrent, src => src.VacationCurrent)
            .Map(dest => dest.VacationBanked, src => src.VacationBanked)
            .Map(dest => dest.ExtraDutyCurrent, src => src.ExtraDutyCurrent)
            .Map(dest => dest.ExtraDutyBanked, src => src.ExtraDutyBanked)
            .Map(dest => dest.VacationUsed, src => src.VacationUsed)
            .Map(dest => dest.VacationCurrentRemaining, src => src.VacationCurrentRemaining)
            .Map(dest => dest.VacationBankedRemaining, src => src.VacationBankedRemaining)
            .Map(dest => dest.ExtraDutyCurrentRemaining, src => src.ExtraDutyCurrentRemaining)
            .Map(dest => dest.ExtraDutyBankedRemaining, src => src.ExtraDutyBankedRemaining)
            .Map(dest => dest.Rate, src => src.Rate)
            .Map(dest => dest.TotalCurrent, src => src.TotalCurrent)
            .Map(dest => dest.TotalBanked, src => src.TotalBanked)
            .Map(dest => dest.TotalPayout, src => src.TotalPayout);
    }
}