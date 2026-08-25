using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Infrastructure.Validation;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Core.Infrastructure;
using Scv.Models.Order;

namespace Scv.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class OrdersController(
    IValidator<OrderRequestDto> orderRequestValidator,
    IValidator<OrderReviewDto> orderReviewValidator,
    IOrderService orderService) : ControllerBase
{
    private readonly IValidator<OrderRequestDto> _orderRequestValidator = orderRequestValidator;
    private readonly IValidator<OrderReviewDto> _orderReviewValidator = orderReviewValidator;
    private readonly IOrderService _orderService = orderService;

    /// <summary>
    /// Retrieves all orders assigned to the judge.
    /// </summary>
    /// <param name="judgeId">The override judge id.</param>
    /// <returns>List of orders for the judge.</returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    public async Task<IActionResult> GetMyOrders(int? judgeId = null)
    {
        var judgeOrders = await _orderService.GetJudgeOrdersAsync(this.User.JudgeId(judgeId));
        return Ok(judgeOrders);
    }

    /// <summary>
    /// Retrieves a specific order by its ID for the judge.
    /// </summary>
    /// <param name="id">The ID of the order.</param>
    /// <param name="judgeId">The override judge id.</param>
    /// <returns>The order details.</returns>
    [HttpGet]
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("{id}")]
    public async Task<IActionResult> GetOrder([FromRoute, ObjectId] string id, int? judgeId = null)
    {
        var judgeOrder = await _orderService.GetOrderByIdAsync(id, this.User.JudgeId(judgeId));
        if (judgeOrder == null)
        {
            return NotFound("Order not found.");
        }
        return Ok(judgeOrder);
    }

    /// <summary>
    /// Create/Update an order to notify that there is a document requiring annotation for a judge. This endpoint is used by external systems to create or update orders.
    /// </summary>
    /// <param name="orderRequestDto">The Order payload (supports snake_case, PascalCase, camelCase and case-insensitive)</param>
    /// <returns>Processed order</returns>
    [HttpPut]
    [Authorize(AuthenticationSchemes = CsoPolicies.AuthenticationScheme, Policy = CsoPolicies.RequireWriteRole)]
    [ProducesResponseType(typeof(OperationResult<OrderDto>), 200)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpsertOrder([FromBody] OrderRequestDto orderRequestDto)
    {
        var basicValidation = await _orderRequestValidator.ValidateAsync(orderRequestDto);

        if (!basicValidation.IsValid)
        {
            return UnprocessableEntity(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessValidation = await _orderService.ValidateOrderRequestAsync(orderRequestDto);
        if (!businessValidation.Succeeded)
        {
            return UnprocessableEntity(new { error = businessValidation.Errors });
        }

        var result = await _orderService.ProcessOrderRequestAsync(orderRequestDto);
        if (!result.Succeeded)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Errors });
        }

        return Ok(result);
    }

    /// <summary>
    /// Partially updates an order's status, signature, comments and document info.
    /// </summary>
    /// <param name="id">The order id</param>
    /// <param name="orderReview">The order review payload</param>
    /// <returns>No content on success</returns>
    [HttpPatch]
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("{id}/review")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReviewOrder(
        [FromRoute, ObjectId] string id,
        [FromBody] OrderReviewDto orderReview)
    {
        var basicValidation = await _orderReviewValidator.ValidateAsync(orderReview);
        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var result = await _orderService.ReviewOrder(id, orderReview);

        if (!result.Succeeded)
        {
            return result.Errors.Any(e => e.Contains("not found"))
                ? NotFound(new { error = result.Errors })
                : StatusCode(StatusCodes.Status500InternalServerError, new { error = result.Errors });
        }

        return NoContent();
    }
}
