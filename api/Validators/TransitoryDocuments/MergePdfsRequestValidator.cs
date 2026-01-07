using FluentValidation;
using Microsoft.Extensions.Options;
using Scv.Models;
using Scv.Models.TransitoryDocuments;
using System.Linq;

namespace Scv.Api.Validators.TransitoryDocuments;

public class MergePdfsRequestValidator : AbstractValidator<MergePdfsRequest>
{
    public MergePdfsRequestValidator(IOptions<TdApiOptions> tdApiOptions)
    {
        var maxFileSize = tdApiOptions.Value.MaxFileSize;
        var maxSizeMB = maxFileSize / 1024.0 / 1024.0;

        RuleFor(x => x.Files)
            .NotNull().WithMessage("files are required and must contain at least one file.")
            .NotEmpty().WithMessage("files are required and must contain at least one file.");

        RuleForEach(x => x.Files)
            .ChildRules(file =>
            {
                file.RuleFor(f => f.RelativePath)
                    .NotEmpty().WithMessage("All files must have a valid RelativePath.");

                file.RuleFor(f => f.SizeBytes)
                    .GreaterThanOrEqualTo(0).WithMessage("All files must have SizeBytes greater than or equal to 0.");
            })
            .When(x => x.Files != null);

        RuleFor(x => x.Files)
            .Must((request, files) =>
            {
                if (files == null) return true;
                long totalSize = files.Sum(f => f.SizeBytes);
                return totalSize <= maxFileSize;
            })
            .WithMessage((request, files) =>
            {
                if (files == null) return string.Empty;
                long totalSize = files.Sum(f => f.SizeBytes);
                var totalSizeMB = totalSize / 1024.0 / 1024.0;
                return $"Total file size ({totalSizeMB:F2} MB) exceeds maximum allowed size of {maxSizeMB:F2} MB.";
            })
            .When(x => x.Files != null);
    }
}