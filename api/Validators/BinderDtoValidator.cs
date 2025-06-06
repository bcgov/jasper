using FluentValidation;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Validators;

public class BinderDtoValidator : BaseDtoValidator<BinderDto>
{
    public BinderDtoValidator() : base()
    {
        RuleFor(r => r.Id)
            .Must((dto, id, context) =>
            {
                if (!IsEdit(context))
                {
                    return true;
                }

                var fileId = context.RootContextData["FileId"] as string;
                return fileId == dto.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);
            });
    }
}
