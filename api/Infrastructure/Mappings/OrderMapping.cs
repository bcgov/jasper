using System;
using System.Globalization;
using CSOCommon.Models;
using Mapster;
using Scv.Db.Models;
using Scv.Models.Order;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Infrastructure.Mappings;

public class OrderMapping : IRegister
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.CreatedDate, src => src.Ent_Dtm)
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm)
            .Map(dest => dest.DocumentData, src => ToBase64OrNull(src.DocumentData))
            .Map(dest => dest.SupportingDocumentData, src => ToBase64OrNull(src.SupportingDocumentData));

        config.NewConfig<OrderDto, Order>()
            .Ignore(dest => dest.Id)
            .Map(dest => dest.DocumentData, src => FromBase64OrNull(src.DocumentData))
            .Map(dest => dest.SupportingDocumentData, src => FromBase64OrNull(src.SupportingDocumentData))
            .Ignore(dest => dest.Ent_Dtm)
            .Ignore(dest => dest.Ent_UserId)
            .Ignore(dest => dest.Upd_Dtm)
            .Ignore(dest => dest.Upd_UserId)
            .Ignore(dest => dest.ReassignmentNotificationsSent)
            .Ignore(dest => dest.ReminderNotificationsSent);

        config.NewConfig<OrderReviewDto, OrderDto>()
            .IgnoreNullValues(true)
            .Map(dest => dest.ProcessedDate, src => DateTime.UtcNow)
            .Map(dest => dest.UpdatedDate, src => DateTime.UtcNow);

        config.NewConfig<Order, OrderViewDto>()
            .Map(dest => dest.CourtFileNumber, src => src.OrderRequest.FullFileNo)
            .Map(dest => dest.PhysicalFileId, src => src.OrderRequest.PhysicalFileId)
            .Map(dest => dest.CourtClass, src => src.OrderRequest.CourtClassCd)
            .Map(dest => dest.PackageId, src => src.OrderRequest.Referral.PackageId)
            .Map(dest => dest.PackageDocumentId, src => src.OrderRequest.Referral.ReferredDocumentId)
            .Map(dest => dest.PriorityType, src => src.OrderRequest.Referral.PriorityType)
            .Map(dest => dest.CourtListType, src => src.OrderRequest.Referral.CourtListTypeCd)
            .Map(dest => dest.ReferralNotes, src => src.OrderRequest.Referral.ReferralNotesTxt)
            .Map(dest => dest.ReceivedDate, src => src.Ent_Dtm.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture))
            .AfterMapping((src, dest) =>
            {
                dest.ProcessedDate = src.ProcessedDate?.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
            });

        config.NewConfig<OrderDto, JudicialAction>()
            .Map(dest => dest.SignatureApplied, src => src.Signed)
            .Map(dest => dest.Comment, src => src.Comments)
            .Map(dest => dest.Document, src => GetDocumentData(src))
            .Map(dest => dest.OrderTerms, _ => Array.Empty<OrderTerm>())
            .AfterMapping((src, dest) =>
            {
                dest.ActionDate = src.ProcessedDate.HasValue ? src.ProcessedDate.Value : default;
                dest.DecisionCode = src.Status switch
                {
                    OrderStatus.Approved => nameof(JudicialDecisionCd.APPR),
                    OrderStatus.Unapproved => nameof(JudicialDecisionCd.NAPP),
                    OrderStatus.AwaitingDocumentation => nameof(JudicialDecisionCd.AFDC),
                    _ => null,
                };
            });
    }

    void IRegister.Register(TypeAdapterConfig config)
    {
        Register(config);
    }

    private static string ToBase64OrNull(byte[] data) =>
        data is { Length: > 0 } ? Convert.ToBase64String(data) : null;

    private static byte[] FromBase64OrNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? [] : Convert.FromBase64String(value);

    private static byte[] FromBase64OrThrow(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"'{fieldName}' is required for order submission but was null or empty.");
        }

        try
        {
            return Convert.FromBase64String(value);
        }
        catch (FormatException ex)
        {
            throw new InvalidOperationException($"'{fieldName}' contains invalid base64 content and cannot be decoded.", ex);
        }
    }

    private static byte[] GetDocumentData(OrderDto src)
    {
        if (src.Status != OrderStatus.Approved)
        {
            return null;
        }

        var data = !string.IsNullOrWhiteSpace(src.DocumentData)
            ? src.DocumentData
            : src.SupportingDocumentData;

        return FromBase64OrThrow(data, nameof(src.DocumentData));
    }
}
