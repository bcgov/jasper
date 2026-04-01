using System;
using System.Globalization;
using Mapster;
using Scv.Api.Models.Order;
using Scv.Db.Models;
using PCSSCommonConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Infrastructure.Mappings;

public class OrderMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
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
            .Map(dest => dest.CourtFileNumber, src => src.OrderRequest.CourtFile.FullFileNo)
            .Map(dest => dest.PhysicalFileId, src => src.OrderRequest.CourtFile.PhysicalFileId)
            .Map(dest => dest.CourtClass, src => src.OrderRequest.CourtFile.CourtClassCd)
            .Map(dest => dest.StyleOfCause, src => src.OrderRequest.CourtFile.StyleOfCause)
            .Map(dest => dest.PackageId, src => src.OrderRequest.Referral.PackageId)
            .Map(dest => dest.PackageDocumentId, src => src.OrderRequest.Referral.ReferredDocumentId)
            .Map(dest => dest.ReceivedDate, src => src.Ent_Dtm.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture))
            .AfterMapping((src, dest) =>
            {
                dest.ProcessedDate = src.ProcessedDate?.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
            });

        config.NewConfig<OrderDto, OrderActionDto>()
            .Map(dest => dest.ReferredDocumentId, src => src.OrderRequest.Referral.ReferredDocumentId.GetValueOrDefault())
            .Map(dest => dest.ReviewedByAgenId, src => src.OrderRequest.Referral.ReferredByAgenId)
            .Map(dest => dest.ReviewedByPartId, src => src.OrderRequest.Referral.ReferredByPartId)
            .Map(dest => dest.ReviewedByPaasSeqNo, src => src.OrderRequest.Referral.ReferredByPaasSeqNo)
            .Map(dest => dest.SentToAgenId, src => src.OrderRequest.Referral.SentToAgenId)
            .Map(dest => dest.SentToPartId, src => src.OrderRequest.Referral.SentToPartId)
            .Map(dest => dest.DigitalSignatureApplied, src => src.Signed)
            .Map(dest => dest.CommentTxt, src => src.Comments)
            .Map(dest => dest.PdfObject, src => !string.IsNullOrWhiteSpace(src.DocumentData) ? src.DocumentData : src.SupportingDocumentData)
            .Map(dest => dest.OrderTerms, _ => Array.Empty<OrderTerm>())
            .AfterMapping((src, dest) =>
            {
                dest.JudicialActionDt = src.ProcessedDate.HasValue
                    ? src.ProcessedDate.Value.ToString(CultureInfo.InvariantCulture)
                    : null;

                dest.JudicialDecisionCd = src.Status switch
                {
                    OrderStatus.Approved => nameof(JudicialDecisionCd.APPR),
                    OrderStatus.Unapproved => nameof(JudicialDecisionCd.NAPP),
                    OrderStatus.AwaitingDocumentation => nameof(JudicialDecisionCd.AFDC),
                    _ => null,
                };
            });
    }

    private static string ToBase64OrNull(byte[] data) =>
        data is { Length: > 0 } ? Convert.ToBase64String(data) : null;

    private static byte[] FromBase64OrNull(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : Convert.FromBase64String(value);
}