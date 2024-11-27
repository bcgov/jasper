
namespace PCSSClient.Clients.JudicialCalendarsServices
{
    using PCSS.Models.REST.JudicialCalendar;
    using System = global::System;

    public partial class JudicialCalendarsServicesClient
    {
        private HttpClient _httpClient;
        private Lazy<Newtonsoft.Json.JsonSerializerSettings> _settings;

        public JudicialCalendarsServicesClient(System.Net.Http.HttpClient httpClient)
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

        public async Task<ICollection<JudicialCalendar>> JudicialCalendarsGetAsync(string locationId, DateTime startDate, DateTime endDate, System.Threading.CancellationToken cancellationToken)
        {
            var requestUrl = $"<baseURL>/api/v2/calendar/judges?locationIds={locationId}&startDate={startDate:dd-MMM-yyyy}&endDate={endDate:dd-MMM-yyyy}";
            var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
            request.Headers.Add("Authorization", "Basic " + Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes("Login:Password")));

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(responseContent);
            //var judicialCalendars = new List<JudicialCalendar>();
            var judicialCalendars = jsonResponse["calendars"].ToObject<List<JudicialCalendar>>();
            var locationIds = locationId.Split(',').ToList();
            judicialCalendars = judicialCalendars?.Where(t => t.Days?.Count > 0).ToList();
            return judicialCalendars!=null ? judicialCalendars : new List<JudicialCalendar>();
        }
    }
}
