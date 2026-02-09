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
            .Map(dest => dest.ProcessedDate, src => src.ProcessedDate.HasValue
                ? src.ProcessedDate.Value.ToString(PCSSCommonConstants.DATE_FORMAT, CultureInfo.InvariantCulture)
                : null);
        
        config.NewConfig<Order, ReviewedOrderDto>()
            .Map(dest => dest.ReferredDocumentId, src => src.OrderRequest.Referral.ReferredDocumentId)
            .Map(dest => dest.JudicialActionDt, src => src.ProcessedDate ?? DateTime.UtcNow)
            .Map(dest => dest.CommentTxt, src => src.Comments)
            .Map(dest => dest.ReviewedByAgenId, src => (int?)null)
            .Map(dest => dest.ReviewedByPartId, src => (int?)null)
            .Map(dest => dest.ReviewedByPassSeqNo, src => (int?)null)
            .Map(dest => dest.JudicialDecisionCd, src => (JudicialDecisionCode)src.Status)
            .Map(dest => dest.DigitalSignatureApplied, src => src.Signed)
            .Map(dest => dest.RejectedDt, src => 
                src.Status == OrderStatus.Unapproved && src.ProcessedDate.HasValue 
                    ? src.ProcessedDate 
                    : null)
            .Map(dest => dest.SignedDt, src => src.Status == OrderStatus.Approved ? src.ProcessedDate : null)
            .Map(dest => dest.PdfObject, src => src.DocumentData);
    }
}
