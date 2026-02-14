using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Scv.Api.Infrastructure.Authentication;
using Scv.Api.Infrastructure.Options;
using Scv.Api.Models.Order;

namespace Scv.Api.Controllers;


[Route("api/[controller]")]
[ApiController]
public class MockOrdersController(
    IValidator<OrderActionDto> orderActionValidator,
    IKeycloakTokenService tokenService,
    IOptions<KeycloakClientOptions> keycloakClientOptions,
    ILogger<MockOrdersController> logger) : ControllerBase
{
    private readonly IValidator<OrderActionDto> _orderActionValidator = orderActionValidator;
    private readonly IKeycloakTokenService _tokenService = tokenService;
    private readonly KeycloakClientOptions _keycloakClientOptions = keycloakClientOptions.Value;
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
        var authHeader = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("Missing or invalid Authorization header for mock order endpoint.");
            return Unauthorized();
        }

        var providedToken = authHeader.Substring("Bearer ".Length).Trim();
        if (string.IsNullOrWhiteSpace(providedToken))
        {
            _logger.LogWarning("Empty bearer token received for mock order endpoint.");
            return Unauthorized();
        }

        var expectedToken = await _tokenService.GetServiceAccountTokenAsync(
            _keycloakClientOptions,
            HttpContext.RequestAborted);

        if (!string.Equals(providedToken, expectedToken, System.StringComparison.Ordinal))
        {
            _logger.LogWarning("Bearer token mismatch for mock order endpoint.");
            return Unauthorized();
        }

        _logger.LogInformation("Received order action payload: {Payload}", JsonConvert.SerializeObject(orderActionDto));

        var basicValidation = await _orderActionValidator.ValidateAsync(orderActionDto);

        if (!basicValidation.IsValid)
        {
            return UnprocessableEntity(basicValidation.Errors.Select(e => e.ErrorMessage));
        }

        return Ok();
    }
}


