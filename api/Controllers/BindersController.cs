using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MongoDB.Bson;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class BindersController(
    IBinderService binderService,
    IBinderFactory binderFactory,
    IValidator<BinderDto> basicValidator) : ControllerBase
{
    private readonly IBinderService _binderService = binderService;
    private readonly IBinderFactory _binderFactory = binderFactory;
    private readonly IValidator<BinderDto> _basicValidator = basicValidator;

    [HttpGet]
    public async Task<IActionResult> GetBinders()
    {
        var dto = new BinderDto
        {
            Labels = this.Request.Query.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToString())
        };

        var basicValidation = await _basicValidator.ValidateAsync(dto);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var binderProcessor = _binderFactory.Generate(dto);

        await binderProcessor.PreProcessAsync(dto);

        var binders = await _binderService.GetByLabels(dto.Labels);

        return Ok(binders);
    }

    [HttpPost]
    public async Task<IActionResult> CreateBinder(BinderDto dto)
    {
        // Basic validation
        var basicValidation = await _basicValidator.ValidateAsync(dto);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        // Get appropriate processor
        var binderProcessor = _binderFactory.Generate(dto);

        // Prep binder
        await binderProcessor.PreProcessAsync(dto);

        // Business rules validation related to Processor
        var processorValidation = await binderProcessor.ValidateAsync(dto);
        if (!processorValidation.Succeeded)
        {
            return BadRequest(new { error = processorValidation.Errors });
        }

        // Persist
        var result = await _binderService.AddAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        var routeValues = new RouteValueDictionary(result.Payload.Labels);

        return CreatedAtAction(nameof(GetBinders), routeValues, result.Payload);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBinder(string id, BinderDto dto)
    {
        // Basic validation
        var context = new ValidationContext<BinderDto>(dto)
        {
            RootContextData =
            {
                ["RouteId"] = id,
                ["IsEdit"] = true
            },
        };
        var basicValidation = await _basicValidator.ValidateAsync(context);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        // Get appropriate processor
        var binderProcessor = _binderFactory.Generate(dto);

        // Business rules validation related to processor
        var processorValidation = await binderProcessor.ValidateAsync(dto);
        if (!processorValidation.Succeeded)
        {
            return BadRequest(new { error = processorValidation.Errors });
        }

        // Since business rules passed, prep binder
        await binderProcessor.PreProcessAsync(dto);

        // Persist
        var result = await _binderService.UpdateAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result);
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest("Invalid ID.");
        }

        var binderToDelete = await _binderService.GetByIdAsync(id);
        var binderProcessor = _binderFactory.Generate(binderToDelete);

        // Ensure that current user can only delete his own binder
        var processorValidation = await binderProcessor.ValidateAsync(binderToDelete);
        if (!processorValidation.Succeeded)
        {
            return BadRequest(new { error = processorValidation.Errors });
        }

        var result = await _binderService.DeleteAsync(id);
        if (!result.Succeeded)
        {

            return BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }
}
