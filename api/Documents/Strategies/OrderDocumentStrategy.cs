using System;
using System.IO;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Nutrient.NativeSDK.API.Exceptions;
using Scv.Core.ContractResolver;
using Scv.Core.Helpers.Extensions;
using Scv.Models.Document;

namespace Scv.Api.Documents.Strategies
{
    public class OrderDocumentStrategy : IDocumentStrategy
    {
        private readonly IJudicialServicesClient _judicialClient;
        private readonly IConfiguration _configuration;

        public DocumentType Type => DocumentType.Order;

        public OrderDocumentStrategy(IJudicialServicesClient judicialClient, IConfiguration configuration)
        {
            _judicialClient = judicialClient;
            _judicialClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _configuration = configuration;
        }

        public async Task<MemoryStream> Invoke(PdfDocumentRequestDetails documentRequest)
        {
            var documentResponseStreamCopy = new MemoryStream();

            var transactionId = Guid.NewGuid();
            documentRequest.CorrelationId ??= transactionId.ToString();
            var isValidAgencyId = double.TryParse(_configuration.GetNonEmptyValue("Request:AgencyIdentifierId"), out var agencyId);
            if (!isValidAgencyId)
            {
                throw new InvalidArgumentException("Invalid agency id");
            }

            var isValidDocumentId = double.TryParse(documentRequest.DocumentId, out var documentId);
            if (!isValidDocumentId)
            {
                throw new InvalidArgumentException("Invalid document id");
            }

            using var response = await _judicialClient.GetJudicialDocumentAsync(
                transactionId,
                agencyId,
                documentId,
                DocumentApplicationName.CSO,
                _configuration.GetNonEmptyValue("Request:ApplicationCd"),
                "Y");

            await response.Stream.CopyToAsync(documentResponseStreamCopy);

            return documentResponseStreamCopy;
        }
    }
}
