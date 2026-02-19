using System;
using FluentValidation;
using Scv.Api.Models.Order;

namespace Scv.Api.Validators.Order;

public class OrderActionDtoValidator : AbstractValidator<OrderActionDto>
{
    public OrderActionDtoValidator()
    {
        RuleFor(r => r.ReferredDocumentId)
            .NotNull().WithMessage("ReferredDocumentId is required.");

        RuleFor(r => r.CommentTxt)
           .NotNull().WithMessage("CommentTxt is required.");

        RuleFor(r => r.JudicialDecisionCd)
            .NotEmpty().WithMessage("JudicialDecisionCd is required.")
            .Must(value => Enum.TryParse<JudicialDecisionCd>(value, out _))
            .WithMessage("JudicialDecisionCd must be one of: APPR, NAPP, AFDC.");

        RuleFor(r => r.ReviewedByAgenId)
            .NotEmpty().WithMessage("ReviewedByAgenId is required.")
            .When(r => r.ReviewedByPartId == null && r.ReviewedByPaasSeqNo == null);

        RuleFor(r => r.ReviewedByPartId)
            .NotEmpty().WithMessage("ReviewedByPartId is required.")
            .When(r => r.ReviewedByAgenId == null && r.ReviewedByPaasSeqNo == null);

        RuleFor(r => r.ReviewedByPaasSeqNo)
            .NotEmpty().WithMessage("ReviewedByPaasSeqNo is required.")
            .When(r => r.ReviewedByAgenId == null && r.ReviewedByPartId == null);

        RuleFor(r => r.RejectedDt)
            .NotEmpty().WithMessage("RejectedDt or SignedDt is required.")
            .When(r => r.SignedDt == null);

        RuleFor(r => r.SignedDt)
            .NotEmpty().WithMessage("SignedDt or RejectedDt is required.")
            .When(r => r.RejectedDt == null);
    }
}
