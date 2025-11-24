using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Constants;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class CourtSummaryReportStrategy : IDocumentStrategy
{
    private readonly FileServicesClient _filesClient;
    private readonly ClaimsPrincipal _currentUser;
    private readonly IConfiguration _configuration;

    public CourtSummaryReportStrategy(IConfiguration configuration, FileServicesClient filesClient, ClaimsPrincipal currentUser)
    {
        _filesClient = filesClient;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _currentUser = currentUser;
        _configuration = configuration;
    }

    public DocumentType Type => DocumentType.CourtSummary;

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        var documentResponse = await _filesClient.FilesCivilCourtsummaryreportAsync(
            _currentUser.AgencyCode(),
            _currentUser.ParticipantId(),
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            documentRequest.AppearanceId,
            JustinReportName.CEISR035);

        var result = new MemoryStream(Convert.FromBase64String(documentResponse.ReportContent));

        return result;
    }
}