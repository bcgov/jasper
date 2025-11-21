using Scv.Models.Location;
using System.Collections.Generic;
using System.Threading.Tasks;
using JCRegion = JCCommon.Clients.LocationServices.Region;

namespace Scv.Api.Services;

/// <summary>
/// Interface for location service to enable testing.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Gets all locations.
    /// </summary>
    /// <param name="includeChildRecords">Whether to include child records.</param>
    /// <returns>Collection of locations.</returns>
    Task<ICollection<Location>> GetLocations(bool includeChildRecords = false);

    /// <summary>
    /// Gets the location short name by location ID.
    /// </summary>
    /// <param name="locationId">The location ID.</param>
    /// <returns>The location short name.</returns>
    Task<string> GetLocationShortName(string locationId);

    /// <summary>
    /// Gets the location name by code.
    /// </summary>
    /// <param name="code">The location code.</param>
    /// <returns>The location name.</returns>
    Task<string> GetLocationName(string code);

    /// <summary>
    /// Gets the location code from ID.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The location code.</returns>
    Task<string> GetLocationCodeFromId(string code);

    /// <summary>
    /// Gets the location ID from code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The location ID.</returns>
    Task<string> GetLocationId(string code);

    /// <summary>
    /// Gets the location agency identifier.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The agency identifier.</returns>
    Task<string> GetLocationAgencyIdentifier(string code);

    /// <summary>
    /// Gets the region name by code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The region name.</returns>
    Task<string> GetRegionName(string code);

    /// <summary>
    /// Gets the region by code.
    /// </summary>
    /// <param name="code">The code.</param>
    /// <returns>The region.</returns>
    Task<JCRegion> GetRegion(string code);

    /// <summary>
    /// Gets the location code by agency identifier code.
    /// </summary>
    /// <param name="agencyIdentifierCd">The agency identifier code.</param>
    /// <returns>The location code.</returns>
    Task<string> GetLocationCodeByAgencyIdentifierCd(string agencyIdentifierCd);
}
