namespace Scv.Core.Helpers
{
    public static class XForwardedForHelper
    {
        public static string BuildUrlString(string forwardedHost, string forwardedPort, string baseUrl, string remainingPath = "", string query = "")
        {
            var sanitizedPath = baseUrl;
            if (!string.IsNullOrEmpty(remainingPath))
            {
                sanitizedPath = string.Format("{0}/{1}", baseUrl.TrimEnd('/'), remainingPath.TrimStart('/'));
            }

            var uriBuilder = new UriBuilder
            {
                Scheme = "https",
                Host = forwardedHost,
                Path = sanitizedPath,
                Query = query
            };

            var portComponent =
                string.IsNullOrEmpty(forwardedPort) || forwardedPort == "80" || forwardedPort == "443"
                    ? ""
                    : $":{forwardedPort}";

            if (!string.IsNullOrEmpty(portComponent))
            {
                int port;
                int.TryParse(forwardedPort, out port);
                uriBuilder.Port = port;
            }

            return uriBuilder.Uri.AbsoluteUri;
        }
    }
}
