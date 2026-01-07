using FluentValidation;
using Scv.Api.Models.Dars;
using Scv.Models.Dars;

namespace Scv.Api.Validators.Dars;

public class TranscriptSearchRequestValidator : AbstractValidator<TranscriptSearchRequest>
{
    public TranscriptSearchRequestValidator()
    {
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.PhysicalFileId) || !string.IsNullOrWhiteSpace(x.MdocJustinNo))
            .WithMessage("Either PhysicalFileId or MdocJustinNo must be provided.");

        RuleFor(x => x)
            .Must(x => string.IsNullOrWhiteSpace(x.PhysicalFileId) || string.IsNullOrWhiteSpace(x.MdocJustinNo))
            .WithMessage("Only one of PhysicalFileId or MdocJustinNo can be provided at a time.");

    }
}
