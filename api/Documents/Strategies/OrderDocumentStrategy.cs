using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using LazyCache;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
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

    public DocumentType Type => DocumentType.Order;

    public OrderDocumentStrategy(
        IJudicialServicesClient judicialClient,
        IConfiguration configuration,
        IRepositoryBase<Order> orderRepository,
        IHttpClientFactory httpClientFactory,
        IPdfPageEditor pdfPageEditor,
        IAppCache cache)
    {
        _judicialClient = judicialClient;
        _judicialClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _configuration = configuration;
        _orderRepository = orderRepository;
        _httpClientFactory = httpClientFactory;
        _pdfPageEditor = pdfPageEditor;
        _cache = cache;
    }

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        ArgumentNullException.ThrowIfNullOrWhiteSpace(documentRequest.DocumentId);
        ArgumentNullException.ThrowIfNullOrWhiteSpace(documentRequest.OrderId);

        if (!double.TryParse(_configuration.GetNonEmptyValue("Request:AgencyIdentifierId"), out var agencyId))
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

        return await AppendSmallClaimsAdditionalPage(documentStream);
    }

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
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            "Y");

        var documentStream = new MemoryStream();
        await response.Stream.CopyToAsync(documentStream);
        return documentStream;
    }

    private async Task<MemoryStream> AppendSmallClaimsAdditionalPage(MemoryStream documentStream)
    {
        var url = _configuration.GetNonEmptyValue("ORDER_SCL029_URL");
        ArgumentNullException.ThrowIfNullOrWhiteSpace(url);

        var client = _httpClientFactory.CreateClient();

        async Task<HttpResponseMessage> smallClaimsPage() => await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

        using var responseTask = _cache.GetOrAddAsync("SmallClaimsPage", smallClaimsPage);

        responseTask.Result.EnsureSuccessStatusCode();

        using var additionalPageStream = new MemoryStream();
        await responseTask.Result.Content.CopyToAsync(additionalPageStream);

        var mergedStream = _pdfPageEditor.AppendPages(documentStream, additionalPageStream);

        await documentStream.DisposeAsync();
        return mergedStream;
    }
}
