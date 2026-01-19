using System;
using FluentValidation;
using JCCommon.Clients.FileServices;
using Scv.Api.Models.Order;

namespace Scv.Api.Validators.Order;

public class OrderRequestDtoValidator : AbstractValidator<OrderRequestDto>
{
    public OrderRequestDtoValidator()
    {
        RuleFor(r => r.CourtFile)
            .NotNull().WithMessage("CourtFile is required.");

        RuleFor(r => r.CourtFile.PhysicalFileId)
           .NotNull().WithMessage("PhysicalFileId is required.")
           .When(r => r.CourtFile != null);

        RuleFor(r => r.CourtFile.CourtDivisionCd)
            .NotEmpty().WithMessage("CourtDivisionCd is required.")
            .Must(BeValidCourtDivision)
            .WithMessage("CourtDivisionCd is unsupported.")
            .When(r => r.CourtFile != null);

        RuleFor(r => r.CourtFile.CourtClassCd)
            .NotEmpty().WithMessage("CourtClassCd is required.")
            .Must(BeValidCourtClass)
            .WithMessage("CourtClassCd is unsupported.")
            .When(r => r.CourtFile != null);

        RuleFor(r => r.Referral)
            .NotNull().WithMessage("Referral is required.");

        RuleFor(r => r.Referral.SentToPartId)
          .NotNull().WithMessage("SentToPartId is required.")
          .When(r => r.Referral != null);

        RuleFor(r => r.PackageDocuments)
            .NotNull().WithMessage("PackageDocuments is required.")
            .NotEmpty().WithMessage("PackageDocuments cannot be empty.");
    }

    private static bool BeValidCourtDivision(string division)
    {
        if (string.IsNullOrWhiteSpace(division))
        {
            return false;
        }

        if (!Enum.TryParse<FileDetailPcssCourtDivisionCd>(division, true, out var divisionEnum))
        {
            return false;
        }

        return divisionEnum == FileDetailPcssCourtDivisionCd.R
            || divisionEnum == FileDetailPcssCourtDivisionCd.I;
    }

    private static bool BeValidCourtClass(string courtClass)
    {
        if (string.IsNullOrWhiteSpace(courtClass))
        {
            return false;
        }

        if (!Enum.TryParse<CourtClassCd>(courtClass, true, out var courtClassEnum))
        {
            return false;
        }

        return courtClassEnum switch
        {
            CourtClassCd.C
                or CourtClassCd.F
                or CourtClassCd.L
                or CourtClassCd.M
                or CourtClassCd.A
                or CourtClassCd.Y
                or CourtClassCd.T => true,
            _ => false,
        };
    }
}
