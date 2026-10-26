using Mapster;
using Scv.Db.Models;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Infrastructure.Mappings;

public class UserArtifactMapping : IRegister
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<UserArtifactDto, UserArtifact>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Ent_Dtm)
            .Ignore(dest => dest.Ent_UserId)
            .Ignore(dest => dest.Upd_Dtm)
            .Ignore(dest => dest.Upd_UserId)
            .Ignore(dest => dest.DeletedDate)
            .Ignore(dest => dest.DeletedByUserId)
            .Include<NoteDto, Note>()
            .Include<AnnotationDto, Annotation>();

        config.NewConfig<UserArtifact, UserArtifactDto>()
            .Map(dest => dest.CreatedDate, src => src.Ent_Dtm)
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm)
            .Include<Note, NoteDto>()
            .Include<Annotation, AnnotationDto>();

        config.NewConfig<NoteDto, Note>()
            .Inherits<UserArtifactDto, UserArtifact>();
        config.NewConfig<Note, NoteDto>()
            .Inherits<UserArtifact, UserArtifactDto>();

        config.NewConfig<AnnotationDto, Annotation>()
            .Inherits<UserArtifactDto, UserArtifact>();
        config.NewConfig<Annotation, AnnotationDto>()
            .Inherits<UserArtifact, UserArtifactDto>();
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }
}
