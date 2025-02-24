using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

namespace tests.api.Services
{
    public abstract class ServiceTestBase
    {
        protected readonly Mock<HttpMessageHandler> MockHandler;
        protected readonly Mock<IHttpClientFactory> MockHttpClientFactory;
        protected readonly HttpClient HttpClient;

        protected ServiceTestBase()
        {
            this.MockHandler = new Mock<HttpMessageHandler>();

            var faker = new Faker();
            this.HttpClient = new HttpClient(MockHandler.Object)
            {
                BaseAddress = new Uri(faker.Internet.Url())
            };

            this.MockHttpClientFactory = new Mock<IHttpClientFactory>();
            this.MockHttpClientFactory
                .Setup(_ => _.CreateClient(It.IsAny<string>()))
                .Returns(HttpClient);
        }

        protected void SetupMockResponse(HttpStatusCode statusCode, object response)
        {
            var jsonResponse = JsonConvert.SerializeObject(response);

            this.MockHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(jsonResponse, Encoding.UTF8, "application/json")
                });
        }

        protected void VerifyHttpRequest(HttpMethod method, string urlFragment)
        {
            this.MockHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == method &&
                        req.RequestUri.ToString().Contains(urlFragment)
                    ),
                    ItExpr.IsAny<CancellationToken>()
                );
        }
    }
}
