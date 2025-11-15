using System;
using System.Collections.Generic;
using System.Linq;

namespace Scv.Api.Models.Location;

public class Location
{
    private const string BASE_URL = "https://provincialcourt.bc.ca/court-locations/";
    private static readonly string[] invalidWords = ["Law", "Courts", "Court", "Provincial"];
    private static readonly string[] courtTypes = ["Provincial Courts", "Provincial Court", "Law Courts"];

    public string Name { get; set; }
    public string ShortName { get; set; }
    public string Code { get; set; }
    public string LocationId { get; set; }
    public string AgencyIdentifierCd { get; set; }
    public bool? Active { get; set; }
    public Uri InfoLink => ParseCourtLocationUrl(Name);
    public ICollection<CourtRoom> CourtRooms { get; set; }

    private Location(string name, string code, string locationId, bool? active, ICollection<CourtRoom> courtRooms)
    {
        Name = name;
        Code = code;
        LocationId = locationId;
        Active = active;
        CourtRooms = courtRooms;
    }

    private Location(string name, string shortName, string code, string locationId, string agencyIdentifierCd, bool? active, ICollection<CourtRoom> courtRooms)
        : this(name, code, locationId, active, courtRooms)
    {
        AgencyIdentifierCd = agencyIdentifierCd;
        // When short name is not available, use the location name and make it a short name
        ShortName = string.IsNullOrWhiteSpace(shortName) ? GenerateShortName(name) : shortName;
    }

    public static Location Create(string name, string code, string locationId, bool? active, ICollection<CourtRoom> courtRooms)
    {
        return new Location(name, code, locationId, active, courtRooms);
    }

    public static Location Create(Location jcLocation, Location pcssLocation)
    {
        return new Location(
            jcLocation.Name,
            pcssLocation?.Name,
            jcLocation.LocationId,
            pcssLocation?.LocationId,
            jcLocation.Code,
            pcssLocation != null ? pcssLocation.Active : jcLocation.Active,
            pcssLocation != null ? pcssLocation.CourtRooms : jcLocation.CourtRooms);
    }

    private static Uri ParseCourtLocationUrl(string locationName)
    {
        if (string.IsNullOrEmpty(locationName))
        {
            return null;
        }

        var filteredWords = locationName.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !invalidWords.Contains(word, StringComparer.OrdinalIgnoreCase));

        locationName = string.Join("-", filteredWords);

        return new Uri(BASE_URL + locationName.ToLower());
    }

    private static string GenerateShortName(string name)
    {
        string result = name;

        foreach (var phrase in courtTypes)
        {
            result = result.Replace(phrase, "", StringComparison.OrdinalIgnoreCase);
        }

        return string.Join(" ", result.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}