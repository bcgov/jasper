using FluentValidation;
using Scv.Api.Models;
using Scv.Core.Helpers;

namespace Scv.Api.Validators.Order;

public class SupportingDocumentUploadRequestValidator : AbstractValidator<SupportingDocumentUploadRequest>
{
    public SupportingDocumentUploadRequestValidator()
    {
        RuleFor(r => r.File)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("No file uploaded.")
            .Must(file => file is { Length: > 0 }).WithMessage("No file uploaded.")
            .Must(file =>
            {
                using var stream = file.OpenReadStream();
                return DocumentHelper.IsDocXDocument(stream);
            }).WithMessage("Supporting document must be a valid Word (.docx) document.");
    }
}
