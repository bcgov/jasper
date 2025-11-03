using DnsClient.Internal;
using Microsoft.Extensions.Logging;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;

namespace Scv.Api.Services
{
    public class TransitoryDocumentsService : ITransitoryDocumentsService
    {
        private readonly TransitoryDocumentsClient _tdClient;
        private readonly LocationService _locationService;
        private readonly Lazy<JsonSerializerOptions> _jsonSerializerOptions;
        private readonly ILogger<TransitoryDocumentsService> _logger;

        public TransitoryDocumentsService(
            ILogger<TransitoryDocumentsService> logger,
            TransitoryDocumentsClient transitoryDocumentsClient,
            LocationService locationService)
        {
            _jsonSerializerOptions = new Lazy<JsonSerializerOptions>(CreateJsonSerializerOptions);
            _logger = logger;
            _tdClient = transitoryDocumentsClient;
            _locationService = locationService;
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
        /// <param name="bearer">The bearer token for authentication.</param>
        /// <param name="locationId">The location to retrieve files.</param>
        /// <param name="roomCode">The room within the location.</param>
        /// <param name="date">The date to retrieve files.</param>
        /// <returns>The collection of file metadata from the API.</returns>
        /// <exception cref="ApiException">A server-side error occurred.</exception>
        public async Task<IEnumerable<FileMetadataDto>> ListSharedDocuments(string bearer, string locationId, string roomCode, string date)
        {
            _logger.LogInformation("Searching for documents in location: {Location}, room: {Room}, date: {Date}", locationId, roomCode, date);
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
                return await _tdClient.SearchAsync(new TransitoryDocumentSearchRequest() { Date = parsedDate, RoomCd = roomCode, AgencyIdentifierCd = matchingLocation.AgencyIdentifierCd, LocationShortName = locationShortName, RegionCode = region.RegionId.ToString(), RegionName = region.RegionName});
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
        /// <param name="bearer">The bearer token for authentication.</param>
        /// <param name="path">The absolute UNC path to the file (will be normalized to relative path).</param>
        /// <returns>A file stream response containing the stream, file name, and content type.</returns>
        public async Task<FileStreamResponse> DownloadFile(string bearer, string path)
        {
            _logger.LogInformation("Downloading file from path: {Path}", path);
            _tdClient.SetBearerToken(bearer);

            try
            {
                // Normalize the UNC-style path to the relative format expected by the TD API
                var normalizedPath = NormalizePathForTdApi(path);
                _logger.LogDebug("Normalized path from '{Original}' to '{Normalized}'", path, normalizedPath);

                var fileResponse = await _tdClient.ContentAsync(normalizedPath);

                // Extract file name from Content-Disposition header if available
                var fileName = GetFileNameFromHeaders(fileResponse.Headers, path);

                // Extract content type from headers
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

        /// <summary>
        /// Normalizes a UNC-style path to the relative format expected by the Transitory Documents API.
        /// Example input: "Criminal Share\Fraser+Vancouver Coastal\222 main\2025\10 October\October 1 (Wed)\101\File.pdf"
        /// Example output: "Fraser+Vancouver Coastal\222 main\2025\10 October\October 1 (Wed)\101\File.pdf"
        /// </summary>
        private string NormalizePathForTdApi(string uncPath)
        {
            if (string.IsNullOrWhiteSpace(uncPath))
                return uncPath;

            var normalized = uncPath.Replace('/', '\\').Trim('\\');

            var segments = normalized.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
                return uncPath;

            var firstSegment = segments[0];
            if (firstSegment.EndsWith(" Share", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Removing share prefix: {SharePrefix}", firstSegment);
                segments = segments.Skip(1).ToArray();
            }

            return string.Join("\\", segments);
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
                    var fileNameMatch = System.Text.RegularExpressions.Regex.Match(
                        disposition,
                        @"filename\*?=[""']?(?:UTF-\d+'')?([^""';]+)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);

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