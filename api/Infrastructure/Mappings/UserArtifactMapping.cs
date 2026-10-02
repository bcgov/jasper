using Mapster;

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

        config.NewConfig<NoteDto, Note>();
        config.NewConfig<Note, NoteDto>();

        config.NewConfig<AnnotationDto, Annotation>();
        config.NewConfig<Annotation, AnnotationDto>();
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }
}
