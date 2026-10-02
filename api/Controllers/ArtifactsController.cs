using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Scv.Api.Infrastructure.Authorization;
using Scv.Models.UserArtifacts;

namespace Scv.Api.Controllers;

/// <summary>
/// Single CRUD/search surface for all user artifacts (Notes and Annotations). The concrete type is
/// resolved from the "artifactType" field of the request body - see <see cref="UserArtifactDtoConverter"/>.
/// </summary>
[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/artifacts")]
[ApiController]
public class ArtifactsController(
    IUserArtifactService artifactService,
    IValidator<NoteDto> noteValidator,
    IValidator<AnnotationDto> annotationValidator) : ControllerBase
{
    private readonly IUserArtifactService _artifactService = artifactService;
    private readonly IValidator<NoteDto> _noteValidator = noteValidator;
    private readonly IValidator<AnnotationDto> _annotationValidator = annotationValidator;

    [HttpPost]
    public async Task<IActionResult> Create(UserArtifactDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Request body is required.");
        }

        var basicValidation = await ValidateBasicAsync(dto);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await _artifactService.ValidateAsync(dto);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await _artifactService.AddAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Payload.Id }, result.Payload);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, [FromQuery] bool includeDeleted = false)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest("Invalid ID.");
        }

        var dto = await _artifactService.GetByIdAsync(id, includeDeleted);
        if (dto == null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UserArtifactDto dto)
    {
        if (dto == null)
        {
            return BadRequest("Request body is required.");
        }

        var basicValidation = await ValidateBasicAsync(dto, id);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await _artifactService.ValidateAsync(dto, true);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await _artifactService.UpdateAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result.Payload);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest("Invalid ID.");
        }

        var result = await _artifactService.DeleteAsync(id);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return NoContent();
    }

    /// <summary>
    /// Unified search/filter/list across Notes and Annotations. Accepts POST for broad client
    /// compatibility, and the HTTP QUERY method (RFC 10008) for clients/tools that support it.
    /// </summary>
    [AcceptVerbs("POST", "QUERY")]
    [Route("search")]
    public async Task<IActionResult> Search(SearchArtifactsCriteria criteria)
    {
        var result = await _artifactService.SearchAsync(criteria ?? new SearchArtifactsCriteria());
        return Ok(result);
    }

    private async Task<FluentValidation.Results.ValidationResult> ValidateBasicAsync(UserArtifactDto dto, string routeId = null)
    {
        var isEdit = routeId != null;

        switch (dto)
        {
            case NoteDto note:
                return await _noteValidator.ValidateAsync(BuildContext(note, routeId, isEdit));
            case AnnotationDto annotation:
                return await _annotationValidator.ValidateAsync(BuildContext(annotation, routeId, isEdit));
            default:
                var result = new FluentValidation.Results.ValidationResult();
                result.Errors.Add(new FluentValidation.Results.ValidationFailure(
                    nameof(UserArtifactDto.ArtifactType),
                    "Unsupported or missing artifact type."));
                return result;
        }
    }

    private static ValidationContext<TDto> BuildContext<TDto>(TDto dto, string routeId, bool isEdit)
        where TDto : UserArtifactDto
    {
        return new ValidationContext<TDto>(dto)
        {
            RootContextData =
            {
                ["RouteId"] = routeId,
                ["IsEdit"] = isEdit
            }
        };
    }
}
