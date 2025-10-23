using System;
using System.Globalization;
using Mapster;
using PCSSCommon.Models;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Models;
using Scv.Db.Models;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Infrastructure.Mappings;

public class ReservedJudgementMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CsvReservedJudgement, ReservedJudgement>();
        config.NewConfig<ReservedJudgement, ReservedJudgementDto>()
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm);
        config.NewConfig<Case, ReservedJudgementDto>()
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
            .Map(dest => dest.DueDate, src => DateTime.ParseExact(
                src.NextApprDt,
                PCSSCommonConstants.DATE_FORMAT,
                CultureInfo.InvariantCulture));
        config.NewConfig<ReservedJudgementDto, ReservedJudgement>()
             .Ignore(dest => dest.Id);
    }
}
