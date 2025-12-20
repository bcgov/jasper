using System.Net.Http;
using System.Net.Http.Headers;

namespace DARSCommon.Clients.TranscriptsServices;

public partial class TranscriptsServicesClient
{
    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        // Fix for GetAttachmentBaseAsync requesting JSON instead of binary
        // The generated client sets Accept: application/json, which causes the AWS Lambda proxy
        // to treat the response as JSON and stringify the binary content, corrupting the PDF.
        if (url.EndsWith("/attachment"))
        {
            request.Headers.Accept.Clear();
            request.Headers.Accept.Add(MediaTypeWithQualityHeaderValue.Parse("application/octet-stream"));
        }
    }
}

