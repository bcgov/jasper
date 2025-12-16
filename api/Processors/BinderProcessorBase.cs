using FluentValidation;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Db.Contants;
using Scv.Models;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Processors;

public interface IBinderProcessor
{
    public BinderDto Binder { get; }
    Task<OperationResult> ValidateAsync();
    Task PreProcessAsync();
    Task<OperationResult> ProcessAsync();
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

        // Sort documents
        this.Binder.Documents = [.. this.Binder.Documents
            .OrderBy(d => d.Order)
            .Select((doc, index) => { doc.Order = index; return doc; })];

        return Task.CompletedTask;
    }

    public virtual Task<OperationResult> ProcessAsync()
    {
        throw new NotImplementedException();
    }

    public virtual async Task<OperationResult> ValidateAsync()
    {
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

        return OperationResult.Success();
    }
}