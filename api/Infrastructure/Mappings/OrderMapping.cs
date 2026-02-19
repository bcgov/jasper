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
            .Map(dest => dest.UpdatedDate, src => src.Upd_Dtm);

        config.NewConfig<OrderDto, Order>()
            .Ignore(dest => dest.Id)
            .Ignore(dest => dest.Ent_Dtm)
            .Ignore(dest => dest.Ent_UserId)
            .Ignore(dest => dest.Upd_Dtm)
            .Ignore(dest => dest.Upd_UserId);

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
                if (src.ProcessedDate.HasValue)
                {
                    dest.ProcessedDate = src.ProcessedDate.Value.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture);
                }
                else
                {
                    dest.ProcessedDate = null;
                }
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
            .Map(dest => dest.PdfObject, src => src.DocumentData)
            .Map(dest => dest.OrderTerms, _ => Array.Empty<OrderTerm>())
            .AfterMapping((src, dest) =>
            {
                dest.JudicialActionDt = src.ProcessedDate.HasValue
                    ? src.ProcessedDate.Value.ToString(CultureInfo.InvariantCulture)
                    : null;

                switch (src.Status)
                {
                    case OrderStatus.Approved:
                        dest.JudicialDecisionCd = nameof(JudicialDecisionCd.APPR);
                        break;
                    case OrderStatus.Unapproved:
                        dest.JudicialDecisionCd = nameof(JudicialDecisionCd.NAPP);
                        break;
                    case OrderStatus.Pending:
                        dest.JudicialDecisionCd = nameof(JudicialDecisionCd.AFDC);
                        break;
                    default:
                        dest.JudicialDecisionCd = null;
                        break;
                }
            });
    }
}