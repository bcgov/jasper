using System.Net;
using System.Collections.Generic;
using System;
using Bogus;
using Scv.Api.Helpers;
using Xunit;
using Scv.Core.Helpers;

namespace tests.api.Helpers
{
    public class XForwardedForHelperTests
    {
        [Fact]
        public void BuildUrlString_ShouldRemoveDoubleSlashesInUrlPath()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var path2 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 8080;
            var expected = $"https://{host}:{port}/{path1}/{path2}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", $"{path1}///", $"//{path2}");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldRemoveDoubleSlashesInUrlPathWithManyForwardSlash()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var path2 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 80;
            var expected = $"https://{host}/{path1}/{path2}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", $"{path1}//////////////", $"///////////////{path2}");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldExcludeWhenPortIs443()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var path2 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 443;
            var expected = $"https://{host}/{path1}/{path2}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", path1, path2);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldExcludeWhenPortIs80()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var path2 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 80;
            var expected = $"https://{host}/{path1}/{path2}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", path1, path2);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldReturnCorrectURLWhenNoOtherUrlPath()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 80;
            var expected = $"https://{host}/{path1}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", path1);

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldReturnCorrectURLWithQueryParams()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var path1 = WebUtility.UrlEncode(faker.Random.Word());
            var path2 = WebUtility.UrlEncode(faker.Random.Word());
            var port = 80;
            string param1 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Random.Number(1, 100)}");
            string param2 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Internet.UserName()}");
            string param3 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Random.Bool()}");

            var expected = $"https://{host}/{path1}/{path2}?{param1}&{param2}&{param3}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", path1, path2, $"{param1}&{param2}&{param3}");

            Assert.Equal(expected, result);
        }

        [Fact]
        public void BuildUrlString_ShouldReturnCorrectURLRandomPaths()
        {
            var faker = new Faker();
            var host = faker.Internet.DomainName();
            var basePath = WebUtility.UrlEncode(faker.Random.Word());
            var remainingPathCount = faker.Random.Number(1, 5);
            var paths = new List<string>();
            var port = 80;

            string param1 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Random.Number(1, 100)}");
            string param2 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Internet.UserName()}");
            string param3 = WebUtility.UrlEncode($"{faker.Lorem.Word()}={faker.Random.Bool()}");

            for (int i = 0; i < remainingPathCount; i++)
            {
                paths.Add(WebUtility.UrlEncode(faker.Random.Word()));
            }

            var expected = $"https://{host}/{basePath}/{string.Join("/", paths)}?{param1}&{param2}&{param3}";

            var result = XForwardedForHelper.BuildUrlString(host, $"{port}", basePath, string.Join("/", paths), $"{param1}&{param2}&{param3}");

            Console.WriteLine(expected);

            Assert.Equal(expected, result);
        }
    }
}