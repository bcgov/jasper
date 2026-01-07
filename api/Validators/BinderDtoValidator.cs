using FluentValidation;
using Scv.Db.Contants;
using Scv.Models;
using System.Collections.Generic;

namespace Scv.Api.Validators;

public class BinderDtoValidator : BaseDtoValidator<BinderDto>
{
    public BinderDtoValidator() : base()
    {
        RuleFor(r => r.Labels)
            .Must(HaveRequiredLabels)
            .WithMessage($"Labels must contain {LabelConstants.PHYSICAL_FILE_ID} and {LabelConstants.COURT_CLASS_CD}");
    }

    private static bool HaveRequiredLabels(Dictionary<string, string> labels)
    {
        return labels.Count != 0
            && labels.ContainsKey(LabelConstants.PHYSICAL_FILE_ID)
            && labels.ContainsKey(LabelConstants.COURT_CLASS_CD);
    }
}
