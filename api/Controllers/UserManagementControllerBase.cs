﻿using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using Scv.Api.Models.UserManagement;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

/// <summary>
/// Base Controller class for handling requests related to User Management (Permission, Role, Group and User)
/// </summary>
/// <typeparam name="TService">The main service class used in this class.</typeparam>
/// <typeparam name="TDto">The DTO type used in this class.</typeparam>
/// <param name="service">Instance of TService</param>
/// <param name="validator">Validator for DTO type</param>
public abstract class UserManagementControllerBase<TService, TDto>(
    TService service,
    IValidator<TDto> validator) : ControllerBase
    where TService : IUserManagementService<TDto>
    where TDto : UserManagementDto
{
    public TService Service { get; } = service;
    public IValidator<TDto> Validator { get; } = validator;

    protected virtual async Task<IActionResult> GetAll()
    {
        return Ok(await this.Service.GetAllAsync());
    }

    protected virtual async Task<IActionResult> GetById(string id)
    {
        if (!ObjectId.TryParse(id.ToString(), out _))
        {
            return BadRequest("Invalid ID.");
        }

        var dto = await this.Service.GetByIdAsync(id);
        if (dto == null)
        {
            return NotFound();
        }

        return Ok(dto);
    }

    protected virtual async Task<IActionResult> Create(TDto role)
    {
        var basicValidation = await this.Validator.ValidateAsync(role);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await this.Service.ValidateAsync(role);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await this.Service.AddAsync(role);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Payload.Id }, result.Payload);
    }

    protected async Task<IActionResult> Update(string id, TDto dto)
    {
        var context = new ValidationContext<TDto>(dto)
        {
            RootContextData =
            {
                ["RouteId"] = id,
                ["IsEdit"] = true
            },
        };

        var basicValidation = await this.Validator.ValidateAsync(context);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessRulesValidation = await this.Service.ValidateAsync(dto, true);
        if (!businessRulesValidation.Succeeded)
        {
            return BadRequest(new { error = businessRulesValidation.Errors });
        }

        var result = await this.Service.UpdateAsync(dto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result.Payload);
    }

    protected async Task<IActionResult> Delete(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return BadRequest("Invalid ID.");
        }

        var result = await this.Service.DeleteAsync(id);
        if (!result.Succeeded)
        {

            return BadRequest(new { errors = result.Errors });
        }

        return NoContent();
    }
}
