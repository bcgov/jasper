using System;
using System.IO;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Nutrient.NativeSDK.API.Exceptions;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Models.Document;

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
            var isValidGuid = Guid.TryParse(documentRequest.DocumentId, out var guid);
            if (!isValidGuid)
            {
                throw new InvalidArgumentException("Invalid correlation id");
            }

            documentRequest.CorrelationId ??= Guid.NewGuid().ToString();
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
                guid,
                documentId,
                agencyId,
                DocumentApplicationName.CSO,
                "JASPER",
                true.ToString());

            await response.Stream.CopyToAsync(documentResponseStreamCopy);

            return documentResponseStreamCopy;
        }
    }
}
