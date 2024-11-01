using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Scv.Api.Helpers;

namespace Scv.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public TestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        [Route("Headers")]
        [AllowAnonymous]

        public ActionResult Headers()
        {
            return Ok(Request.Headers);
        }

        [HttpGet]
        [Route("APIG")]
        [AllowAnonymous]
        public async Task<ActionResult> TestAPIG()
        {
            var baseUrl = _configuration.GetNonEmptyValue("AWS_API_GATEWAY_URL");
            var apiKey = _configuration.GetNonEmptyValue("AWS_API_GATEWAY_API_KEY");

            Console.WriteLine(baseUrl);
            using var client = new HttpClient();
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-api-key", apiKey);
            // client.DefaultRequestHeaders.Add("x-origin-verify", "1234456");

            var response = await client.GetStringAsync("locations/rooms");
            return Ok(response);
        }
    }
}
