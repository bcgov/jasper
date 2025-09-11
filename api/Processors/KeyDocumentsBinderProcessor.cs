using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public class KeyDocumentsBinderProcessor(
    ClaimsPrincipal currentUser,
    IValidator<BinderDto> basicValidator,
    BinderDto dto) : BinderProcessorBase(currentUser, dto, basicValidator)
{
    public override Task PreProcessAsync()
    {
        // Key Documents Binder are generated in the backend so we have full control
        // which data are saved. Overriding this method so Labels aren't cleared.
        return Task.CompletedTask;
    }

    public override async Task<OperationResult> ValidateAsync()
    {
        var result = await base.ValidateAsync();
        if (!result.Succeeded)
        {
            return result;
        }

        var errors = new List<string>();

        var requiredKeys = new[]
        {
            LabelConstants.PARTICIPANT_ID,
            LabelConstants.PROF_SEQ_NUMBER,
            LabelConstants.COURT_LEVEL_CD,
            LabelConstants.COURT_CLASS_CD,
            LabelConstants.APPEARANCE_ID,
            LabelConstants.PHYSICAL_FILE_ID
        };

        var labels = this.Binder.Labels ?? [];

        foreach (var key in requiredKeys)
        {
            if (!labels.ContainsKey(key))
            {
                errors.Add($"Missing label: {key}");
            }
        }

        return errors.Count != 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }
}
