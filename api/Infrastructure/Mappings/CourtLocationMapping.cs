using System;
using System.Collections.Generic;
using System.Linq;
using Mapster;
using Scv.Models.CourtLocation;
using ParserModel = Scv.Api.Documents.Parsers.Models;

namespace Scv.Api.Infrastructure.Mappings;

public class CourtLocationMapping : IRegister
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<ParserModel.CourtLocation, CourtLocationDto>()
            .Map(dest => dest.IsStaffed, src => src.Staffed == "Staffed")
            .Map(dest => dest.JcmPhones, src => BuildJcmPhones(src))
            .Map(dest => dest.Emails, src => BuildEmails(src))
            .Ignore(dest => dest.AdultProbationOffice)
            .Ignore(dest => dest.YouthProbationOffice);
        config.NewConfig<ParserModel.AdultProbationOffice, AdultProbationOfficeDto>()
            .Map(dest => dest.Phones, src => BuildAdultProbationPhones(src))
            .Map(dest => dest.TollFreePhone, src => BuildPhone(src.TollFree, src.TollFreeNotes));
        config.NewConfig<ParserModel.YouthProbationOffice, YouthProbationOfficeDto>()
            .Map(dest => dest.Phone, src => BuildPhone(src.Phone, src.PhoneNotes));
    }

    private static List<PhoneInfoDto> BuildJcmPhones(ParserModel.CourtLocation src) =>
        BuildPhones(
            (src.JcmPhone1, src.JcmPhone1Notes),
            (src.JcmPhone2, src.JcmPhone2Notes),
            (src.JcmPhone3, src.JcmPhone3Notes),
            (src.JcmPhone4, src.JcmPhone4Notes),
            (src.JcmPhone5, src.JcmPhone5Notes));

    private static List<PhoneInfoDto> BuildAdultProbationPhones(ParserModel.AdultProbationOffice src) =>
        BuildPhones(
            (src.Phone1, src.Phone1Notes),
            (src.Phone2, src.Phone2Notes));

    private static List<PhoneInfoDto> BuildPhones(params (string Value, string Notes)[] pairs) =>
        BuildList(pairs, BuildPhone);

    private static PhoneInfoDto BuildPhone(string phone, string notes) =>
        !string.IsNullOrWhiteSpace(phone)
            ? new PhoneInfoDto { Phone = phone, Notes = notes }
            : null;

    private static List<EmailInfoDto> BuildEmails(ParserModel.CourtLocation src) =>
        BuildList(
        [
            (src.Email1, src.Email1Notes),
            (src.Email2, src.Email2Notes),
        ], (value, notes) => new EmailInfoDto { Email = value, Notes = notes });

    private static List<T> BuildList<T>(
        IEnumerable<(string Value, string Notes)> pairs,
        Func<string, string, T> factory) =>
        [.. pairs
            .Where(p => !string.IsNullOrWhiteSpace(p.Value))
            .Select(p => factory(p.Value, p.Notes))];

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }
}
