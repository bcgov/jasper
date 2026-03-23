using System.Collections.Generic;
using DARSCommon.Models;
using Microsoft.Net.Http.Headers;

namespace Scv.Api.Models.Dars
{
    public class DarsClientSearchResult
    {
        public IEnumerable<DarsSearchResults> Results { get; set; }
        public IEnumerable<SetCookieHeaderValue> Cookies { get; set; }
    }
}
