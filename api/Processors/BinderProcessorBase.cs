using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Models;
using Scv.Db.Contants;

namespace Scv.Api.Processors;

public interface IBinderProcessor
{
    Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto);
    Task PreProcessAsync(BinderDto dto);
}

public abstract class BinderProcessorBase(ClaimsPrincipal currentUser) : IBinderProcessor
{
    public ClaimsPrincipal CurrentUser { get; } = currentUser;

    public virtual Task PreProcessAsync(BinderDto dto)
    {
        var fileId = dto.Labels.GetValue(LabelConstants.PHYSICAL_FILE_ID);

        // Remove existing labels so the standard labels can be added
        dto.Labels.Clear();

        // Add standard labels for a binder
        dto.Labels.Add(LabelConstants.PHYSICAL_FILE_ID, fileId);
        dto.Labels.Add(LabelConstants.JUDGE_ID, this.CurrentUser.UserId());

        // Sort documents
        dto.Documents = dto.Documents
            .OrderBy(d => d.Order)
            .Select((doc, index) => { doc.Order = index; return doc; })
            .ToList();

        return Task.CompletedTask;
    }

    public virtual Task<OperationResult<BinderDto>> ValidateAsync(BinderDto dto)
    {
        var errors = new List<string>();

        // Validate current user is accessing own binder
        var judgeId = dto.Labels.GetValue(LabelConstants.JUDGE_ID);
        if (judgeId != this.CurrentUser.UserId())
        {
            errors.Add("Current user does not have access to this binder.");
        }

        return Task.FromResult(errors.Count != 0
            ? OperationResult<BinderDto>.Failure([.. errors])
            : OperationResult<BinderDto>.Success(dto));
    }
}