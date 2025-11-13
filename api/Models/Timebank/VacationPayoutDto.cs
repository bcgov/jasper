using System;

namespace Scv.Api.Models.Timebank;

public class VacationPayoutDto
{
    public int JudiciaryPersonId { get; set; }
    public int Period { get; set; }
    public DateTimeOffset EffectiveDate { get; set; }
    public string EntitlementCalcType { get; set; }
    public double VacationCurrent { get; set; }
    public double VacationBanked { get; set; }
    public double ExtraDutyCurrent { get; set; }
    public double ExtraDutyBanked { get; set; }
    public double VacationUsed { get; set; }
    public double VacationCurrentRemaining { get; set; }
    public double VacationBankedRemaining { get; set; }
    public double ExtraDutyCurrentRemaining { get; set; }
    public double ExtraDutyBankedRemaining { get; set; }
    public double Rate { get; set; }
    public double TotalCurrent { get; set; }
    public double TotalBanked { get; set; }
    public double TotalPayout { get; set; }

    public static VacationPayoutDto FromVacationPayout(PCSSCommon.Clients.TimebankServices.VacationPayout source)
    {
        return new VacationPayoutDto
        {
            JudiciaryPersonId = source.JudiciaryPersonId,
            Period = source.Period,
            EffectiveDate = source.EffectiveDate,
            EntitlementCalcType = source.EntitlementCalcType,
            VacationCurrent = source.VacationCurrent,
            VacationBanked = source.VacationBanked,
            ExtraDutyCurrent = source.ExtraDutyCurrent,
            ExtraDutyBanked = source.ExtraDutyBanked,
            VacationUsed = source.VacationUsed,
            VacationCurrentRemaining = source.VacationCurrentRemaining,
            VacationBankedRemaining = source.VacationBankedRemaining,
            ExtraDutyCurrentRemaining = source.ExtraDutyCurrentRemaining,
            ExtraDutyBankedRemaining = source.ExtraDutyBankedRemaining,
            Rate = source.Rate,
            TotalCurrent = source.TotalCurrent,
            TotalBanked = source.TotalBanked,
            TotalPayout = source.TotalPayout
        };
    }
}
