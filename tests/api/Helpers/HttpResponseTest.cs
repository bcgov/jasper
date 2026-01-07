using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Helpers;
using Scv.Api.Helpers.Extensions;
using Scv.Core.Helpers;
using Scv.Core.Helpers.Extensions;
using Xunit;

namespace tests.api.Helpers
{
    public class HttpResponseTest
    {
        public static T CheckForValidHttpResponseAndReturnValue<T>(ActionResult<T> actionResult)
        {
            Assert.NotNull(actionResult);
            Assert.NotNull(actionResult.Result);
            var okObjectResult = actionResult.Result as OkObjectResult;
            Assert.NotNull(okObjectResult);
            var result = (T)okObjectResult.Value;
            Assert.NotNull(result);
            return result;
        }

        public static ControllerContext SetupMockControllerContext(IConfiguration configuration)
        {
            var headerDictionary = new HeaderDictionary();
            var response = new Mock<HttpResponse>();
            response.SetupGet(r => r.Headers).Returns(headerDictionary);

            var httpContext = new Mock<HttpContext>();
            httpContext.SetupGet(a => a.Response).Returns(response.Object);

            var claims = new[] {
                new Claim(CustomClaimTypes.ApplicationCode, "SCV"),
                new Claim(CustomClaimTypes.JcParticipantId,  configuration.GetNonEmptyValue("Request:PartId")),
                new Claim(CustomClaimTypes.JcAgencyCode, configuration.GetNonEmptyValue("Request:AgencyIdentifierId")),
                new Claim(CustomClaimTypes.IsSupremeUser, "True"),
            };
            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            httpContext.SetupGet(a => a.User).Returns(principal);

            return new ControllerContext
            {
                HttpContext = httpContext.Object
            };
        }
    }
}