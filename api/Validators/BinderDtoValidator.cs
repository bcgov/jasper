using System.Collections.Generic;
using FluentValidation;
using Scv.Api.Models;
using Scv.Db.Contants;

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
