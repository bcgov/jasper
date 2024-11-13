using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Scv.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        [HttpGet]
        [Route("Headers")]
        [AllowAnonymous]

        public ActionResult Headers()
        {
            return Ok(Request.Headers);
        }
    }
}