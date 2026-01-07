using System.Net.Http.Headers;

namespace TDCommon.Clients.DocumentsServices
{
    public partial class TransitoryDocumentsClient
    {
        private string _accessTokenValue;

        /// <summary>
        /// Sets the custom header value to be added to all requests.
        /// </summary>
        /// <param name="headerValue">The value of the custom header.</param>
        public void SetBearerToken(string bearerToken)
        {
            _accessTokenValue = bearerToken;
        }

        partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
        {
            if (!string.IsNullOrEmpty(_accessTokenValue))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessTokenValue.Replace("Bearer ", ""));
            }
        }
    }
}