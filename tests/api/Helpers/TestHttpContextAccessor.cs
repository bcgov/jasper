using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace tests.api.Helpers
{
    public class TestHttpContextAccessor : IHttpContextAccessor
    {
        public HttpContext HttpContext { get; set; }

        public TestHttpContextAccessor()
        {
            // Initialize HttpContext as needed
            // For example, you can set the User property
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new(ClaimTypes.Name, "your_username"),
                    // Add other claims as needed
                ], "mock"))
            };
        }
    }
}
