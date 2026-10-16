using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using LazyCache;
using JCCommon.Clients.FileServices;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Services;
using Scv.Core.ContractResolver;
using Scv.Core.Exceptions;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Document;
using Scv.Models.Order;

namespace Scv.Api.Documents.Strategies;

public class OrderDocumentStrategy : IDocumentStrategy
{
    private readonly IJudicialServicesClient _judicialClient;
    private readonly IConfiguration _configuration;
    private readonly IRepositoryBase<Order> _orderRepository;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPdfPageEditor _pdfPageEditor;
    private readonly IAppCache _cache;

    private readonly FileServicesClient _filesClient;
    private readonly ILocationService _locationService;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;
    private readonly string _requestPartId;

    public DocumentType Type => DocumentType.Order;

    public const string COURT_FILE_NUMBER_FIELD = "cfn";
    public const string COURT_ROOM_FIELD = "crtnum";
    public const string COURT_LOCATION_FIELD = "location";

    public OrderDocumentStrategy(
        IJudicialServicesClient judicialClient,
        IConfiguration configuration,
        IRepositoryBase<Order> orderRepository,
        IHttpClientFactory httpClientFactory,
        IPdfPageEditor pdfPageEditor,
        IAppCache cache,
        FileServicesClient filesClient,
        ILocationService locationService)
    {
        _judicialClient = judicialClient;
        _judicialClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _configuration = configuration;
        _orderRepository = orderRepository;
        _httpClientFactory = httpClientFactory;
        _pdfPageEditor = pdfPageEditor;
        _cache = cache;
        _filesClient = filesClient;
        _locationService = locationService;
        _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
        _requestPartId = configuration.GetNonEmptyValue("Request:PartId");
    }

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(documentRequest.DocumentId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(documentRequest.OrderId);

        if (!double.TryParse(_requestAgencyIdentifierId, out var agencyId))
        {
            throw new InvalidDataException("Invalid agency id");
        }

        var decodedDocumentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));
        if (!int.TryParse(decodedDocumentId, out var documentIdInt))
        {
            throw new InvalidDataException("Invalid document id.");
        }

        var order = await _orderRepository.GetByIdAsync(documentRequest.OrderId)
            ?? throw new NotFoundException("Order id not found while fetching a document.");

        var request = order.OrderRequest;
        var docAppName = GetDocumentApplicationName(request, documentIdInt);

        var transactionId = Guid.NewGuid();
        documentRequest.CorrelationId ??= transactionId.ToString();

        var documentStream = await GetJudicialDocumentCopy(transactionId, agencyId, documentIdInt, docAppName);

        if (!IsSmallClaimsDeskOrder(request, documentIdInt))
        {
            return documentStream;
        }

        return await AppendSmallClaimsAdditionalPage(documentStream, request);
    }

    #region Private Methods

    private static DocumentApplicationName GetDocumentApplicationName(OrderRequest request, int documentId)
    {
        if (request.PackageDocuments?.Any(pd => pd.DocumentId == documentId) == true)
        {
            return DocumentApplicationName.CSO;
        }

        if (request.RelevantCeisDocuments?.Any(rd => rd.CivilDocumentId == documentId) == true)
        {
            return DocumentApplicationName.CEIS;
        }

        throw new NotFoundException("Document id not found.");
    }

    private static bool IsSmallClaimsDeskOrder(OrderRequest request, int documentId) =>
        request.Referral?.CourtListTypeCd == CourtListTypeDescriptor.PROVINCIAL_COURT_DESK_ORDER_SMALL_CLAIMS_TYPE
        && request.PackageDocuments?.Any(pd => pd.DocumentId == documentId && pd.ReferredDocument) == true;

    private async Task<MemoryStream> GetJudicialDocumentCopy(
        Guid transactionId,
        double agencyId,
        int documentId,
        DocumentApplicationName docAppName)
    {
        using var response = await _judicialClient.GetJudicialDocumentAsync(
            transactionId,
            agencyId,
            documentId,
            docAppName,
            _applicationCode,
            "Y");

        var documentStream = new MemoryStream();
        await response.Stream.CopyToAsync(documentStream);
        return documentStream;
    }

    private async Task<MemoryStream> AppendSmallClaimsAdditionalPage(MemoryStream documentStream, OrderRequest request)
    {
        await using var originalDocumentStream = documentStream;

        var url = _configuration.GetNonEmptyValue("ORDER_SCL029_URL");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        // Cache the downloaded bytes, not the HttpResponseMessage: its content stream can only be read once.
        var pageBytes = await _cache.GetOrAddAsync("SmallClaimsPage", () => DownloadSmallClaimsPage(url));

        using var templateStream = new MemoryStream(pageBytes);

        // Flatten so the prefilled values are static text and not treated as user edits by the frontend.
        using var additionalPageStream = _pdfPageEditor.FillFormFields(
            templateStream,
            await BuildSmallClaimsFieldValues(request),
            flatten: true);

        var mergedStream = _pdfPageEditor.AppendPages(documentStream, additionalPageStream);

        await documentStream.DisposeAsync();
        return mergedStream;
    }

    private async Task<byte[]> DownloadSmallClaimsPage(string url)
    {
        var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    private async Task<Dictionary<string, string>> BuildSmallClaimsFieldValues(OrderRequest request) =>
        new()
        {
            [COURT_FILE_NUMBER_FIELD] = request.FullFileNo,
            [COURT_LOCATION_FIELD] = await GetLocationShortName(request) ?? string.Empty,
            [COURT_ROOM_FIELD] = await GetAppearanceCourtRoom(request) ?? string.Empty
        };

    private async Task<string> GetAppearanceCourtRoom(OrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.InitiatingAppearanceId))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(request.PhysicalFileId);

        if (!Enum.TryParse<FileDetailPcssCourtDivisionCd>(request.CourtDivisionCd, true, out var divisionEnum))
        {
            throw new InvalidDataException($"Invalid court division code: {request.CourtDivisionCd}");
        }

        return divisionEnum switch
        {
            FileDetailPcssCourtDivisionCd.R => await GetCriminalAppearanceCourtRoom(request),
            FileDetailPcssCourtDivisionCd.I => await GetCivilAppearanceCourtRoom(request),
            _ => throw new InvalidDataException($"Unsupported court division code: {request.CourtDivisionCd}")
        };
    }

    private async Task<string> GetCriminalAppearanceCourtRoom(OrderRequest request)
    {
        var appearances = await _filesClient.FilesCriminalAppearancesAsync(
            _requestAgencyIdentifierId,
            _requestPartId,
            _applicationCode,
            FutureYN.Y,
            HistoryYN.Y,
            request.PhysicalFileId.ToString());

        return appearances?.ApprDetail?
            .FirstOrDefault(a => a.AppearanceId == request.InitiatingAppearanceId)?
            .CourtRoomCd;
    }

    private async Task<string> GetCivilAppearanceCourtRoom(OrderRequest request)
    {
        var appearances = await _filesClient.FilesCivilAppearancesAsync(
            _requestAgencyIdentifierId,
            _requestPartId,
            _applicationCode,
            FutureYN.Y,
            HistoryYN.Y,
            request.PhysicalFileId.ToString());

        return appearances?.ApprDetail?
            .FirstOrDefault(a => a.AppearanceId == request.InitiatingAppearanceId)?
            .CourtRoomCd;
    }

    private async Task<string> GetLocationShortName(OrderRequest request)
    {
        if (request.CourtLocationId == null)
        {
            return string.Empty;
        }

        var locationId = await _locationService.GetLocationId(request.CourtLocationId.ToString());
        if (string.IsNullOrWhiteSpace(locationId))
        {
            return string.Empty;
        }

        var shortName = await _locationService.GetLocationShortName(locationId);
        return shortName;
    }

    #endregion
}
