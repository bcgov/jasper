using DnsClient.Internal;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;
using FileMetadataDto = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Services
{
    public partial class TransitoryDocumentsService : ITransitoryDocumentsService
    {
        private readonly ITransitoryDocumentsClientService _tdClient;
        private readonly ILocationService _locationService;
        private readonly IKeycloakTokenService _keycloakTokenService;
        private readonly IMapper _mapper;
        private readonly Lazy<JsonSerializerOptions> _jsonSerializerOptions;
        private readonly ILogger<TransitoryDocumentsService> _logger;

        [GeneratedRegex(@"filename\*?=[""']?(?:UTF-\d+'')?([^""';]+)", RegexOptions.IgnoreCase)]
        private static partial Regex FileNameRegex();

        public TransitoryDocumentsService(
            ILogger<TransitoryDocumentsService> logger,
            ITransitoryDocumentsClientService transitoryDocumentsClientWrapper,
            ILocationService locationService,
            IKeycloakTokenService keycloakTokenService,
            IMapper mapper)
        {
            _jsonSerializerOptions = new Lazy<JsonSerializerOptions>(CreateJsonSerializerOptions);
            _logger = logger;
            _tdClient = transitoryDocumentsClientWrapper;
            _locationService = locationService;
            _keycloakTokenService = keycloakTokenService;
            _mapper = mapper;
        }

        private JsonSerializerOptions CreateJsonSerializerOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            return options;
        }

        public JsonSerializerOptions JsonSerializerOptions => _jsonSerializerOptions.Value;

        /// <summary>
        /// Calls the Transitory Documents API to search for documents.
        /// </summary>
        /// <param name="locationId">The location to retrieve files.</param>
        /// <param name="roomCode">The room within the location.</param>
        /// <param name="date">The date to retrieve files.</param>
        /// <returns>The collection of file metadata from the API.</returns>
        /// <exception cref="ApiException">A server-side error occurred.</exception>
        public async Task<IEnumerable<FileMetadataDto>> ListSharedDocuments(string locationId, string roomCode, string date)
        {
            _logger.LogInformation("Searching for documents in location: {Location}, room: {Room}, date: {Date}", locationId, roomCode, date);

            var bearer = await _keycloakTokenService.GetAccessTokenAsync();
            _tdClient.SetBearerToken(bearer);

            try
            {
                var locations = await this._locationService.GetLocations();
                var matchingLocation = locations.FirstOrDefault(location => location.LocationId == locationId);

                var locationShortName = matchingLocation?.ShortName;
                if (string.IsNullOrWhiteSpace(locationShortName))
                {
                    _logger.LogError("Location not found for locationId: {LocationId}", locationId);
                    throw new BadRequestException("location not found.");
                }
                _logger.LogDebug("Location {LocationShortName} found for locationId: {LocationId}", locationShortName, locationId);

                var region = await _locationService.GetRegion(matchingLocation.AgencyIdentifierCd);
                if (string.IsNullOrWhiteSpace(region?.RegionName))
                {
                    _logger.LogError("Region not found for locationId: {LocationId}", locationId);
                    throw new BadRequestException("Region not found.");
                }
                _logger.LogDebug("Region {Region} found for locationId: {LocationId}", region.RegionName, locationId);

                if (!DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, out DateTimeOffset parsedDate))
                {
                    _logger.LogError("Invalid date format: {Date}", date);
                    throw new BadRequestException("Invalid date format.");
                }

                _logger.LogDebug("Searching documents for Date: {Date}, Room: {Room}, AgencyIdentifierCd: {AgencyIdentifierCd}, LocationShortName: {LocationShortName}, RegionCode: {RegionCode}, RegionName: {RegionName}",
                    parsedDate, roomCode, matchingLocation.AgencyIdentifierCd, locationShortName, region.RegionId, region.RegionName);
                var clientResult = await _tdClient.SearchAsync(new TransitoryDocumentSearchRequest() { Date = parsedDate, RoomCd = roomCode, AgencyIdentifierCd = matchingLocation.AgencyIdentifierCd, LocationShortName = locationShortName, RegionCode = region.RegionId.ToString(), RegionName = region.RegionName });

                // Use Mapster to map generated client DTOs to shared model DTOs
                return _mapper.Map<IEnumerable<FileMetadataDto>>(clientResult);
            }
            catch (ApiException<string> apiEx)
            {
                _logger.LogError(apiEx, "API Exception when calling Transitory Documents API: {Message}, result: {Data}", apiEx.Message, apiEx.Result);
                throw new ApiException(apiEx.Message, apiEx.StatusCode, apiEx.Response, apiEx.Headers, apiEx);
            }
        }

        /// <summary>
        /// Downloads a file from the Transitory Documents API using the generated client.
        /// </summary>
        /// <param name="path">The relative UNC path to the file (will be normalized to relative path).</param>
        /// <returns>A file stream response containing the stream, file name, and content type.</returns>
        public async Task<FileStreamResponse> DownloadFile(string path)
        {
            _logger.LogInformation("Downloading file from path: {Path}", path);

            var bearer = await _keycloakTokenService.GetAccessTokenAsync();
            _tdClient.SetBearerToken(bearer);

            try
            {
                var fileResponse = await _tdClient.ContentAsync(path);

                var fileName = GetFileNameFromHeaders(fileResponse.Headers, path);

                var contentType = GetContentTypeFromHeaders(fileResponse.Headers);

                var response = new FileStreamResponse(fileResponse.Stream, fileName, contentType);

                _logger.LogInformation("File downloaded successfully: {FileName}, ContentType: {ContentType}, Size: {Size} bytes",
                    response.FileName, response.ContentType, response.SizeBytes);

                return response;
            }
            catch (ApiException<string> apiEx)
            {
                _logger.LogError(apiEx, "API Exception when downloading file from path: {Path}. Status: {StatusCode}, Message: {Message}, Data: {Data}", path, apiEx.StatusCode, apiEx.Message, apiEx.Result);

                throw apiEx.StatusCode switch
                {
                    400 => new BadRequestException($"Invalid file path: {apiEx.Result}"),
                    401 => new NotAuthorizedException("Unauthorized access to file."),
                    403 => new NotAuthorizedException("Access to file is forbidden."),
                    404 => new NotFoundException($"File not found: {path}"),
                    _ => new BadRequestException($"Failed to download file: {apiEx.Result}")
                };
            }
        }

        private static string GetFileNameFromHeaders(
            IReadOnlyDictionary<string, IEnumerable<string>> headers,
            string fallbackPath)
        {
            if (headers.TryGetValue("Content-Disposition", out var dispositionValues))
            {
                var disposition = dispositionValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(disposition))
                {
                    var fileNameMatch = FileNameRegex().Match(disposition);

                    if (fileNameMatch.Success)
                    {
                        return Uri.UnescapeDataString(fileNameMatch.Groups[1].Value.Trim('"', '\''));
                    }
                }
            }

            return Path.GetFileName(fallbackPath);
        }

        private static string GetContentTypeFromHeaders(IReadOnlyDictionary<string, IEnumerable<string>> headers)
        {
            if (headers.TryGetValue("Content-Type", out var contentTypeValues))
            {
                var contentType = contentTypeValues.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    // Remove any charset or other parameters
                    var semicolonIndex = contentType.IndexOf(';');
                    return semicolonIndex > 0
                        ? contentType.Substring(0, semicolonIndex).Trim()
                        : contentType.Trim();
                }
            }

            return "application/octet-stream";
        }
    }
}