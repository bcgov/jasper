using System;
using System.Globalization;
using Mapster;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Models;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Infrastructure.Mappings;

public class CaseMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CsvReservedJudgement, Db.Models.Case>();
        config.NewConfig<Db.Models.Case, CaseDto>()
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm);
        config.NewConfig<PCSSCommon.Models.Case, CaseDto>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.AppearanceId, src => src.NextApprId.ToString())
            .Map(dest => dest.AppearanceDate, src => DateTime.ParseExact(
                src.LastApprDt,
                PCSSCommonConstants.DATE_FORMAT,
                CultureInfo.InvariantCulture))
            .Map(dest => dest.CourtClass, src => src.CourtClassCd)
            .Map(dest => dest.CourtFileNumber, src => src.FileNumberTxt)
            .Map(dest => dest.FileNumber, src => $"{src.CourtClassCd}-{src.FileNumberTxt}")
            .Map(dest => dest.Reason, src => src.NextApprReason)
            .Map(dest => dest.PartId, src => src.ProfPartId)
            .Map(dest => dest.DueDate, src => src.NextApprDt)
            .Map(dest => dest.AgeInDays, src => src.CaseAgeDays)
            .Map(dest => dest.PhysicalFileId, src => src.JustinNo ?? src.PhysicalFileId);
        config.NewConfig<CaseDto, Db.Models.Case>()
             .Ignore(dest => dest.Id);
    }
}
