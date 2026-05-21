using System;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using CSOCommon.Clients.JudicialServices;
using CSOCommon.Models;
using Microsoft.AspNetCore.WebUtilities;
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

        public OrderDocumentStrategy(IJudicialServicesClient judicialClient, IConfiguration configuration, ClaimsPrincipal currentUser)
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

            if (string.IsNullOrWhiteSpace(documentRequest.DocumentId))
            {
                throw new InvalidArgumentException("Invalid document id.");
            }

            var decodedDocumentId = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(documentRequest.DocumentId));
            var isValidDocumentId = double.TryParse(decodedDocumentId, out var documentId);
            if (!isValidDocumentId)
            {
                throw new InvalidArgumentException("Invalid document id.");
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
