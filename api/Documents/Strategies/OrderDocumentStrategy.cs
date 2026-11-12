using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly IRepositoryBase<Order> _orderRepository;
    private readonly IPdfPageEditor _pdfPageEditor;

    private readonly ILocationService _locationService;
    private readonly ILogger<OrderDocumentStrategy> _logger;
    private readonly IAppDocumentService _appDocumentService;
    private readonly string _applicationCode;
    private readonly string _requestAgencyIdentifierId;

    public DocumentType Type => DocumentType.Order;

    public const string COURT_FILE_NUMBER_FIELD = "cfn";
    public const string COURT_LOCATION_FIELD = "location";

    public OrderDocumentStrategy(
        IJudicialServicesClient judicialClient,
        IConfiguration configuration,
        IRepositoryBase<Order> orderRepository,
        IPdfPageEditor pdfPageEditor,
        ILocationService locationService,
        IAppDocumentService appDocumentService,
        ILogger<OrderDocumentStrategy> logger)
    {
        _judicialClient = judicialClient;
        _judicialClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _orderRepository = orderRepository;
        _pdfPageEditor = pdfPageEditor;
        _locationService = locationService;
        _logger = logger;
        _appDocumentService = appDocumentService;

        _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
        _requestAgencyIdentifierId = configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
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
        var smallClaimsPageResult = await _appDocumentService.GetAppDocumentByKeyAsync(AppDocument.SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE);
        if (smallClaimsPageResult == null || smallClaimsPageResult.Data == null)
        {
            _logger.LogWarning("Small Claims Additional Page template not found in AppDocumentService. Key: {Key}", AppDocument.SMALL_CLAIMS_ADDTL_DOCUMENT_TEMPLATE);
            await documentStream.DisposeAsync();
            throw new InvalidOperationException("Small Claims Additional Page template is missing.");
        }

        using var templateStream = new MemoryStream(smallClaimsPageResult.Data);

        // Flatten so the prefilled values are static text and not treated as user edits by the frontend.
        using var additionalPageStream = _pdfPageEditor.FillFormFields(
            templateStream,
            await BuildSmallClaimsFieldValues(request),
            flatten: true);

        var mergedStream = _pdfPageEditor.AppendPages(documentStream, additionalPageStream);

        await documentStream.DisposeAsync();
        return mergedStream;
    }

    private async Task<Dictionary<string, string>> BuildSmallClaimsFieldValues(OrderRequest request) =>
        new()
        {
            [COURT_FILE_NUMBER_FIELD] = request.FullFileNo,
            [COURT_LOCATION_FIELD] = await GetLocationShortName(request) ?? string.Empty
        };

    private async Task<string> GetLocationShortName(OrderRequest request)
    {
        if (request.CourtLocationId == null)
        {
            return string.Empty;
        }

        // CourtLocationId is the location code in LocationService.
        return await _locationService.GetLocationShortNameByCode(request.CourtLocationId.ToString())
            ?? string.Empty;
    }

    #endregion
}
