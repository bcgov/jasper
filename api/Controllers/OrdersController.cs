using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.Order;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

// This will be replaced with another OAuth scheme so that the other team can call this API.
[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class OrdersController(
    IValidator<OrderDto> validator,
    IOrderService orderService) : ControllerBase
{
    private readonly IValidator<OrderDto> _validator = validator;
    private readonly IOrderService _orderService = orderService;

    [HttpGet]
    public async Task<IActionResult> GetMyOrders(int? judgeId = null)
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders.Where(o => o.Referral.SentToPartId == this.User.JudgeId(judgeId)));
    }


    /// <summary>
    /// Endpoint used to notify that there is a documnt requiring annotation for a judge.
    /// </summary>
    /// <param name="orderDto">The Order payload</param>
    /// <returns>Processed order</returns>
    [HttpPut]
    public async Task<IActionResult> UpsertOrder([FromBody] OrderDto orderDto)
    {
        var basicValidation = await _validator.ValidateAsync(orderDto);

        if (!basicValidation.IsValid)
        {
            return BadRequest(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        var businessValidation = await _orderService.ValidateAsync(orderDto);
        if (!businessValidation.Succeeded)
        {
            return BadRequest(new { error = businessValidation.Errors });
        }

        var result = await _orderService.UpsertAsync(orderDto);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Errors });
        }

        return Ok(result);
    }
}
