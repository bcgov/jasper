using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Nutrient.NativeSDK.API.Exceptions;
using Scv.Core.ContractResolver;
using Scv.Core.Exceptions;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Db.Repositories;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies;

public class OrderDocumentStrategy : IDocumentStrategy
{
    private readonly IJudicialServicesClient _judicialClient;
    private readonly IConfiguration _configuration;
    private readonly IRepositoryBase<Order> _orderRepository;

    public DocumentType Type => DocumentType.Order;

    public OrderDocumentStrategy(IJudicialServicesClient judicialClient, IConfiguration configuration, IRepositoryBase<Order> orderRepository)
    {
        _judicialClient = judicialClient;
        _judicialClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        _configuration = configuration;
        _orderRepository = orderRepository;
    }

    public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
    {
        if (string.IsNullOrWhiteSpace(documentRequest.DocumentId))
        {
            throw new InvalidArgumentException("Invalid document id.");
        }

        if (!double.TryParse(_configuration.GetNonEmptyValue("Request:AgencyIdentifierId"), out var agencyId))
        {
            throw new InvalidArgumentException("Invalid agency id");
        }

        var decodedDocumentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));
        if (!int.TryParse(decodedDocumentId, out var documentIdInt))
        {
            throw new InvalidArgumentException("Invalid document id.");
        }

        var order = await _orderRepository.GetByIdAsync(documentRequest.OrderId)
            ?? throw new NotFoundException("Order id not found while fetching a document.");

        var request = order.OrderRequest;

        DocumentApplicationName docAppName;
        if (request.PackageDocuments.Any(pd => pd.DocumentId == documentIdInt))
        {
            docAppName = DocumentApplicationName.CSO;
        }
        else if (request.RelevantCeisDocuments.Any(rd => rd.CivilDocumentId == documentIdInt))
        {
            docAppName = DocumentApplicationName.CEIS;
        }
        else
        {
            throw new NotFoundException("Document id not found.");
        }

        var transactionId = Guid.NewGuid();
        documentRequest.CorrelationId ??= transactionId.ToString();

        using var response = await _judicialClient.GetJudicialDocumentAsync(
            transactionId,
            agencyId,
            documentIdInt,
            docAppName,
            _configuration.GetNonEmptyValue("Request:ApplicationCd"),
            "Y");

        var documentResponseStreamCopy = new MemoryStream();
        await response.Stream.CopyToAsync(documentResponseStreamCopy);
        return documentResponseStreamCopy;
    }
}
