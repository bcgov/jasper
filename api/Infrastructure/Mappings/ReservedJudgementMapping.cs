using Mapster;
using Scv.Api.Documents.Parsers.Models;
using Scv.Api.Models;
using Scv.Db.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class ReservedJudgementMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<CsvReservedJudgement, ReservedJudgement>();
        config.NewConfig<ReservedJudgement, ReservedJudgementDto>()
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm);
    }
}
