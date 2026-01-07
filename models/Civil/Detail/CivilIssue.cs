using JCCommon.Clients.FileServices;
// ReSharper disable UnassignedGetOnlyAutoProperty

namespace Scv.Models.Civil.Detail
{

    public class CivilIssue : CvfcIssue2
    {
        public string IssueTypeDesc { get; set; }
        public string IssueResultCdDesc { get; set; }
    }
}