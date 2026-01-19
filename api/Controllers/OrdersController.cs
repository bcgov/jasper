using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Jobs;
using Scv.Api.Models.Order;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

// This will be replaced with another OAuth scheme so that the other team can call this API.
[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class OrdersController(
    IValidator<OrderDto> validator,
    IOrderService orderService,
    ILogger<OrdersController> logger) : ControllerBase
{
    private readonly IValidator<OrderDto> _validator = validator;
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<OrdersController> _logger = logger;

    /// <summary>
    /// Retrieves all orders assigned to the judge.
    /// </summary>
    /// <param name="judgeId">The override judge id.</param>
    /// <returns>List of orders for the judge.</returns>
    [HttpGet]
    public async Task<IActionResult> GetMyOrders(int? judgeId = null)
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders.Where(o => o.Referral.SentToPartId == this.User.JudgeId(judgeId)));
    }

    /// <summary>
    /// Create/Update an order to notify that there is a documnt requiring annotation for a judge.
    /// </summary>
    /// <param name="orderDto">The Order payload (supports snake_case, PascalCase, camelCase and case-insensitive)</param>
    /// <returns>Processed order</returns>
    [HttpPut]
    [ProducesResponseType(typeof(OperationResult<OrderDto>), 200)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertOrder([FromBody] OrderDto orderDto)
    {
        var basicValidation = await _validator.ValidateAsync(orderDto);

        if (!basicValidation.IsValid)
        {
            return UnprocessableEntity(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessValidation = await _orderService.ValidateAsync(orderDto);
        if (!businessValidation.Succeeded)
        {
            return UnprocessableEntity(new { error = businessValidation.Errors });
        }

        var result = await _orderService.UpsertAsync(orderDto);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Errors });
        }

        // Enqueue notification job as a fire-and-forget background task
        BackgroundJob.Enqueue<SendOrderNotificationJob>(
            job => job.Execute(orderDto));

        return Ok(result);
    }
}
