
namespace PCSSClient.Clients.PCSSLocationsServices
{
    using pcss_client.Models;
    using System = global::System;

    public partial class PCSSLocationsServicesClient
    {
        private HttpClient _httpClient;
        private Lazy<Newtonsoft.Json.JsonSerializerSettings> _settings;

        public PCSSLocationsServicesClient(System.Net.Http.HttpClient httpClient)
        {
            _httpClient = httpClient;
            _settings = new System.Lazy<Newtonsoft.Json.JsonSerializerSettings>(CreateSerializerSettings);
        }
        private Newtonsoft.Json.JsonSerializerSettings CreateSerializerSettings()
        {
            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            UpdateJsonSerializerSettings(settings);
            return settings;
        }
        public Newtonsoft.Json.JsonSerializerSettings JsonSerializerSettings { get { return _settings.Value; } }

        partial void UpdateJsonSerializerSettings(Newtonsoft.Json.JsonSerializerSettings settings);

        public async Task<ICollection<PCSSLocation>> LocationsGetAsync(System.Threading.CancellationToken cancellationToken)
        {

             var requestUrl = "<baseURL>/api/locations/";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("Login:Password")));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JArray.Parse(responseContent);
            var locationsList = new List<PCSSLocation>();

            foreach (var location in jsonResponse)
            {
                var locationId = location.Value<int>("locationId");
                var locationNm = location.Value<string>("locationNm");
                var activeYn = location.Value<string>("activeYn");

                locationsList.Add(new PCSSLocation
                {
                    LocationId = locationId,
                    LocationNm = locationNm,
                    ActiveYn = activeYn
                });
            }

            return locationsList;
        }
    }
}
