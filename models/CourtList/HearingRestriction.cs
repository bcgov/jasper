using JCCommon.Clients.FileServices;
using System.Text.RegularExpressions;

namespace Scv.Models.CourtList
{
    public class HearingRestriction : ClHearingRestriction
    {
        public string HearingRestrictionTypeDesc { get; set; }
        public string? AdjInitialsText => !string.IsNullOrEmpty(JudgeName) ? Regex.Replace(JudgeName, @"(?i)(?:^|\s|-)+([^\s-])[^\s-]*(?:(?:\s+)(?:the\s+)?(?:jr|sr|II|2nd|III|3rd|IV|4th)\.?$)?", "$1").ToUpper() : null;
    }
}
