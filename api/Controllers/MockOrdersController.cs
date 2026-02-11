using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Scv.Api.Models.Order;

namespace Scv.Api.Controllers;


[Route("api/[controller]")]
[ApiController]
public class MockOrdersController(
    IValidator<OrderActionDto> orderActionValidator,
    ILogger<MockOrdersController> logger) : ControllerBase
{
    private readonly IValidator<OrderActionDto> _orderActionValidator = orderActionValidator;
    private readonly ILogger<MockOrdersController> _logger = logger;

    /// <summary>
    /// Mock endpoint to simulate a submitted order.
    /// </summary>
    /// <param name="orderActionDto">The order action payload</param>
    /// <returns></returns>
    [HttpPost]
    [Route("action")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReviewOrder([FromBody] OrderActionDto orderActionDto)
    {
        _logger.LogInformation("Received order action payload: {Payload}", JsonConvert.SerializeObject(orderActionDto));

        var basicValidation = await _orderActionValidator.ValidateAsync(orderActionDto);

        if (!basicValidation.IsValid)
        {
            return UnprocessableEntity(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        return Ok();
    }
}


