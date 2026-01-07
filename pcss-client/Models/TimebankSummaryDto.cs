using PCSSCommon.Clients.TimebankServices;

namespace Scv.Models.Timebank;

public class TimebankSummaryDto
{
    public int JudiciaryPersonId { get; set; }
    public string FirstNm { get; set; }
    public string SurnameNm { get; set; }
    public string JudiciaryTypeCd { get; set; }
    public string JudiciaryPositionCd { get; set; }
    public string JudiciaryStatusCd { get; set; }
    public string JudiciaryInactiveReasonCd { get; set; }
    public string EntitlementCalcType { get; set; }
    public string SeniorDayYn { get; set; }
    public int LocationId { get; set; }
    public int Period { get; set; }
    public string ScheduleCompleteYn { get; set; }
    public VacationSummaryDto Vacation { get; set; }
    public JudgmentDaySummaryDto Judgment { get; set; }
    public JudgmentExtraDaySummaryDto JudgmentExtra { get; set; }
    public EducationSummaryDto Education { get; set; }
    public SeniorDaySummaryDto SeniorDay { get; set; }
    public SittingSummaryDto Sitting { get; set; }
    public IllnessSummaryDto Illness { get; set; }
    public ExtraSeniorDaySummaryDto ExtraSeniorDay { get; set; }
    public UnassignedSummaryDto Unassigned { get; set; }
    public TravelCompensationSummaryDto TravelCompensation { get; set; }

    public static TimebankSummaryDto FromTimebankSummary(TimebankSummary source)
    {
        return new TimebankSummaryDto
        {
            JudiciaryPersonId = source.JudiciaryPersonId,
            FirstNm = source.FirstNm,
            SurnameNm = source.SurnameNm,
            JudiciaryTypeCd = source.JudiciaryTypeCd,
            JudiciaryPositionCd = source.JudiciaryPositionCd,
            JudiciaryStatusCd = source.JudiciaryStatusCd,
            JudiciaryInactiveReasonCd = source.JudiciaryInactiveReasonCd,
            EntitlementCalcType = source.EntitlementCalcType,
            SeniorDayYn = source.SeniorDayYn,
            LocationId = source.LocationId,
            Period = source.Period,
            ScheduleCompleteYn = source.ScheduleCompleteYn,
            Vacation = source.Vacation != null ? VacationSummaryDto.FromVacationSummary(source.Vacation) : null,
            Judgment = source.Judgment != null ? JudgmentDaySummaryDto.FromJudgmentDaySummary(source.Judgment) : null,
            JudgmentExtra = source.JudgmentExtra != null ? JudgmentExtraDaySummaryDto.FromJudgmentExtraDaySummary(source.JudgmentExtra) : null,
            Education = source.Education != null ? EducationSummaryDto.FromEducationSummary(source.Education) : null,
            SeniorDay = source.SeniorDay != null ? SeniorDaySummaryDto.FromSeniorDaySummary(source.SeniorDay) : null,
            Sitting = source.Sitting != null ? SittingSummaryDto.FromSittingSummary(source.Sitting) : null,
            Illness = source.Illness != null ? IllnessSummaryDto.FromIllnessSummary(source.Illness) : null,
            ExtraSeniorDay = source.ExtraSeniorDay != null ? ExtraSeniorDaySummaryDto.FromExtraSeniorDaySummary(source.ExtraSeniorDay) : null,
            Unassigned = source.Unassigned != null ? UnassignedSummaryDto.FromUnassignedSummary(source.Unassigned) : null,
            TravelCompensation = source.TravelCompensation != null ? TravelCompensationSummaryDto.FromTravelCompensationSummary(source.TravelCompensation) : null
        };
    }
}

public class VacationSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double VacationScheduled { get; set; }
    public VacationSummaryBucketDto Regular { get; set; }
    public VacationSummaryBucketCarryOverDto RegularCarryOver { get; set; }
    public VacationSummaryBucketDto ExtraDuties { get; set; }
    public VacationSummaryBucketCarryOverDto ExtraDutiesCarryOver { get; set; }
    public double Total { get; set; }
    public double TotalRemaining { get; set; }

    public static VacationSummaryDto FromVacationSummary(VacationSummary source)
    {
        return new VacationSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            VacationScheduled = source.VacationScheduled,
            Regular = source.Regular != null ? VacationSummaryBucketDto.FromVacationSummaryBucket(source.Regular) : null,
            RegularCarryOver = source.RegularCarryOver != null ? VacationSummaryBucketCarryOverDto.FromVacationSummaryBucketCarryOver(source.RegularCarryOver) : null,
            ExtraDuties = source.ExtraDuties != null ? VacationSummaryBucketDto.FromVacationSummaryBucket(source.ExtraDuties) : null,
            ExtraDutiesCarryOver = source.ExtraDutiesCarryOver != null ? VacationSummaryBucketCarryOverDto.FromVacationSummaryBucketCarryOver(source.ExtraDutiesCarryOver) : null,
            Total = source.Total,
            TotalRemaining = source.TotalRemaining
        };
    }
}

public class VacationSummaryBucketDto
{
    public double Jan1Entitlement { get; set; }
    public double Jan1Adjustment { get; set; }
    public double Jan1Total { get; set; }
    public double OtherAdjustment { get; set; }
    public double TotalAdjustment { get; set; }
    public double OtherEntitlement { get; set; }
    public double Total { get; set; }
    public double Remaining { get; set; }

    public static VacationSummaryBucketDto FromVacationSummaryBucket(VacationSummaryBucket source)
    {
        return new VacationSummaryBucketDto
        {
            Jan1Entitlement = source.Jan1Entitlement,
            Jan1Adjustment = source.Jan1Adjustment,
            Jan1Total = source.Jan1Total,
            OtherAdjustment = source.OtherAdjustment,
            TotalAdjustment = source.TotalAdjustment,
            OtherEntitlement = source.OtherEntitlement,
            Total = source.Total,
            Remaining = source.Remaining
        };
    }
}

public class VacationSummaryBucketCarryOverDto
{
    public double Jan1CarryOver { get; set; }
    public double Jan1Adjustment { get; set; }
    public double Jan1Total { get; set; }
    public double OtherAdjustment { get; set; }
    public double TotalAdjustment { get; set; }
    public double OtherCarryOver { get; set; }
    public double Total { get; set; }
    public double Remaining { get; set; }

    public static VacationSummaryBucketCarryOverDto FromVacationSummaryBucketCarryOver(VacationSummaryBucketCarryOver source)
    {
        return new VacationSummaryBucketCarryOverDto
        {
            Jan1CarryOver = source.Jan1CarryOver,
            Jan1Adjustment = source.Jan1Adjustment,
            Jan1Total = source.Jan1Total,
            OtherAdjustment = source.OtherAdjustment,
            TotalAdjustment = source.TotalAdjustment,
            OtherCarryOver = source.OtherCarryOver,
            Total = source.Total,
            Remaining = source.Remaining
        };
    }
}

public class TimebankLineItemDto
{
    public int JudiciaryPersonId { get; set; }
    public int Period { get; set; }
    public string Date { get; set; }
    public string EffectiveDate { get; set; }
    public string ExpiryDate { get; set; }
    public string TimebankEntryTypeCd { get; set; }
    public double Days { get; set; }
    public double Hours { get; set; }
    public double AnnualDays { get; set; }
    public double AnnualHours { get; set; }
    public bool IsActualEntry { get; set; }
    public string ActivityCd { get; set; }
    public string ActivityClassCd { get; set; }
    public bool IsHalfDay { get; set; }
    public bool IsPeriodEntitlementEntry { get; set; }
    public bool IsRolloverEntry { get; set; }
    public bool IsManualEntry { get; set; }
    public string VacationTypeCd { get; set; }
    public string Comments { get; set; }
    public string ActivitySummary { get; set; }
    public string UpdatedOn { get; set; }
    public string UpdatedBy { get; set; }

    public static TimebankLineItemDto FromTimebankLineItem(TimebankLineItem source)
    {
        return new TimebankLineItemDto
        {
            JudiciaryPersonId = source.JudiciaryPersonId,
            Period = source.Period,
            Date = source.Date,
            EffectiveDate = source.EffectiveDate,
            ExpiryDate = source.ExpiryDate,
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Days = source.Days,
            Hours = source.Hours,
            AnnualDays = source.AnnualDays,
            AnnualHours = source.AnnualHours,
            IsActualEntry = source.IsActualEntry,
            ActivityCd = source.ActivityCd,
            ActivityClassCd = source.ActivityClassCd,
            IsHalfDay = source.IsHalfDay,
            IsPeriodEntitlementEntry = source.IsPeriodEntitlementEntry,
            IsRolloverEntry = source.IsRolloverEntry,
            IsManualEntry = source.IsManualEntry,
            VacationTypeCd = source.VacationTypeCd,
            Comments = source.Comments,
            ActivitySummary = source.ActivitySummary,
            UpdatedOn = source.UpdatedOn,
            UpdatedBy = source.UpdatedBy
        };
    }
}

public class TbSummaryFlagDto
{
    public double? Amount { get; set; }
    public string Reason { get; set; }
    public string ShortDescription { get; set; }
    public string Description { get; set; }

    public static TbSummaryFlagDto FromTbSummaryFlag(TbSummaryFlag source)
    {
        return new TbSummaryFlagDto
        {
            Amount = source.Amount,
            Reason = source.Reason,
            ShortDescription = source.ShortDescription,
            Description = source.Description
        };
    }
}

public class JudgmentDaySummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }

    public static JudgmentDaySummaryDto FromJudgmentDaySummary(JudgmentDaySummary source)
    {
        return new JudgmentDaySummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance
        };
    }
}

public class JudgmentExtraDaySummaryDto
{
    public decimal Actual { get; set; }
    public List<JudgmentExtraDayLineItemDto> LineItems { get; set; }

    public static JudgmentExtraDaySummaryDto FromJudgmentExtraDaySummary(JudgmentExtraDaySummary source)
    {
        return new JudgmentExtraDaySummaryDto
        {
            Actual = source.Actual,
            LineItems = source.LineItems?.Select(JudgmentExtraDayLineItemDto.FromJudgmentExtraDayLineItem).ToList()
        };
    }
}

public class JudgmentExtraDayLineItemDto
{
    public string EffectiveDate { get; set; }
    public decimal Days { get; set; }
    public string Comments { get; set; }

    public static JudgmentExtraDayLineItemDto FromJudgmentExtraDayLineItem(JudgmentExtraDayLineItem source)
    {
        return new JudgmentExtraDayLineItemDto
        {
            EffectiveDate = source.EffectiveDate,
            Days = source.Days,
            Comments = source.Comments
        };
    }
}

public class EducationSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }

    public static EducationSummaryDto FromEducationSummary(EducationSummary source)
    {
        return new EducationSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance
        };
    }
}

public class SeniorDaySummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }
    public double Target { get; set; }
    public string TargetComments { get; set; }
    public string TargetUpdatedBy { get; set; }
    public string TargetUpdatedOn { get; set; }

    public static SeniorDaySummaryDto FromSeniorDaySummary(SeniorDaySummary source)
    {
        return new SeniorDaySummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance,
            Target = source.Target,
            TargetComments = source.TargetComments,
            TargetUpdatedBy = source.TargetUpdatedBy,
            TargetUpdatedOn = source.TargetUpdatedOn
        };
    }
}

public class SittingSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }

    public static SittingSummaryDto FromSittingSummary(SittingSummary source)
    {
        return new SittingSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance
        };
    }
}

public class IllnessSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }

    public static IllnessSummaryDto FromIllnessSummary(IllnessSummary source)
    {
        return new IllnessSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance
        };
    }
}

public class ExtraSeniorDaySummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Entitlement { get; set; }
    public double Actual { get; set; }
    public double Balance { get; set; }

    public static ExtraSeniorDaySummaryDto FromExtraSeniorDaySummary(ExtraSeniorDaySummary source)
    {
        return new ExtraSeniorDaySummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Entitlement = source.Entitlement,
            Actual = source.Actual,
            Balance = source.Balance
        };
    }
}

public class UnassignedSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Actual { get; set; }

    public static UnassignedSummaryDto FromUnassignedSummary(UnassignedSummary source)
    {
        return new UnassignedSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Actual = source.Actual
        };
    }
}

public class TravelCompensationSummaryDto
{
    public string TimebankEntryTypeCd { get; set; }
    public int Period { get; set; }
    public string EntitlementCalcType { get; set; }
    public bool IsHours { get; set; }
    public List<TimebankLineItemDto> LineItems { get; set; }
    public List<TbSummaryFlagDto> Flags { get; set; }
    public double Actual { get; set; }

    public static TravelCompensationSummaryDto FromTravelCompensationSummary(TravelCompensationSummary source)
    {
        return new TravelCompensationSummaryDto
        {
            TimebankEntryTypeCd = source.TimebankEntryTypeCd,
            Period = source.Period,
            EntitlementCalcType = source.EntitlementCalcType,
            IsHours = source.IsHours,
            LineItems = source.LineItems?.Select(TimebankLineItemDto.FromTimebankLineItem).ToList(),
            Flags = source.Flags?.Select(TbSummaryFlagDto.FromTbSummaryFlag).ToList(),
            Actual = source.Actual
        };
    }
}
