using Newtonsoft.Json;

namespace CSOCommon.Clients.JudicialServices;

public partial class JudicialServicesClient
{
    static partial void UpdateJsonSerializerSettings(JsonSerializerSettings settings)
    {
        settings.DateFormatString = "yyyy-MM-dd'T'HH:mm:ss.fffK";
    }
}