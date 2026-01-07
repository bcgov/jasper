using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using Scv.Api.Documents;
using Scv.Core.Helpers.ContractResolver;
using Scv.Core.Helpers.Extensions;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Scv.Api.Services.Files
{
    /// <summary>
    /// This is meant to wrap our FileServicesClient. That way we can easily extend the information provided to us by the FileServicesClient.
    /// </summary>
    public class FilesService
    {
        #region Variables

        public readonly CivilFilesService Civil;
        public readonly CriminalFilesService Criminal;
        private readonly FileServicesClient _filesClient;
        private readonly IAppCache _cache;
        private readonly string _applicationCode;
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;

        #endregion Variables

        #region Constructor

        public FilesService(IConfiguration configuration,
            FileServicesClient filesClient,
            IMapper mapper,
            LookupService lookupService,
            LocationService locationService,
            IAppCache cache,
            ClaimsPrincipal claimsPrincipal,
            ILoggerFactory factory,
            IDocumentConverter documentConverter,
            IBinderService binderService)
        {
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:FileExpiryMinutes")) * 60;
            Civil = new CivilFilesService(
                configuration,
                filesClient,
                mapper,
                lookupService,
                locationService,
                _cache,
                claimsPrincipal,
                factory.CreateLogger<CivilFilesService>(),
                binderService);
            Criminal = new CriminalFilesService(configuration, filesClient, mapper, lookupService, locationService, _cache, claimsPrincipal, documentConverter);

            _applicationCode = configuration.GetNonEmptyValue("Request:ApplicationCd");
            _requestAgencyIdentifierId = claimsPrincipal?.AgencyCode() ?? configuration.GetNonEmptyValue("Request:AgencyIdentifierId");
            _requestPartId = claimsPrincipal?.ParticipantId() ?? configuration.GetNonEmptyValue("Request:PartId");
        }

        #endregion Constructor

        #region Methods

        #region Courtlist & Document     

        public async Task<FileResponse> DocumentAsync(string documentId, bool isCriminal, string physicalFileId, string correlationId = null, bool flatten = true)
        {
            if (correlationId == null)
            {
                correlationId = Guid.NewGuid().ToString();
            }

            return await _filesClient.FilesDocumentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, documentId, isCriminal ? "R" : "I", physicalFileId, flatten, correlationId);
        }

        #endregion Courtlist & Document

        #endregion Methods
    }
}