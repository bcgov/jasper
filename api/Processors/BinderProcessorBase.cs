using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public interface IBinderProcessor
{
    public BinderDto Binder { get; }
    Task<OperationResult> ValidateAsync();
    Task PreProcessAsync();
}

public abstract class BinderProcessorBase(
    ClaimsPrincipal currentUser,
    BinderDto binder,
    IValidator<BinderDto> basicValidator) : IBinderProcessor
{
    public ClaimsPrincipal CurrentUser { get; } = currentUser;
    public BinderDto Binder { get; } = binder;
    public IValidator<BinderDto> BasicValidator { get; } = basicValidator;

    public virtual Task PreProcessAsync()
    {
        var fileId = this.Binder.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);

        // Remove existing labels so the standard labels can be added
        this.Binder.Labels.Clear();

        // Add standard labels for a binder
        this.Binder.Labels.Add(LabelConstants.PHYSICAL_FILE_ID, fileId);
        this.Binder.Labels.Add(LabelConstants.JUDGE_ID, this.CurrentUser.UserId());

        // Sort documents
        this.Binder.Documents = this.Binder.Documents
            .OrderBy(d => d.Order)
            .Select((doc, index) => { doc.Order = index; return doc; })
            .ToList();

        return Task.CompletedTask;
    }

    public virtual async Task<OperationResult> ValidateAsync()
    {
        var errors = new List<string>();

        var context = new ValidationContext<BinderDto>(this.Binder);
        if (this.Binder.Id != null)
        {
            context.RootContextData["RouteId"] = this.Binder.Id;
            context.RootContextData["IsEdit"] = true;
        }

        var basicValidation = await this.BasicValidator.ValidateAsync(context);
        if (!basicValidation.IsValid)
        {
            return OperationResult.Failure([.. basicValidation.Errors.Select(e => e.ErrorMessage)]);
        }

        // Validate current user is accessing own binder
        var judgeId = this.Binder.Labels.GetValue(LabelConstants.JUDGE_ID);
        if (judgeId != this.CurrentUser.UserId())
        {
            errors.Add("Current user does not have access to this binder.");
        }

        return errors.Count != 0
            ? OperationResult.Failure([.. errors])
            : OperationResult.Success();
    }
}