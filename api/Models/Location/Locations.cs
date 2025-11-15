using System.Collections.Generic;
using System.Linq;

namespace Scv.Api.Models.Location;

public class Locations : IList<Location>
{
    private readonly List<Location> _locations;

    public Locations()
    {
        _locations = [];
    }

    public Locations(IEnumerable<Location> locations)
    {
        _locations = [.. locations];
    }

    public Location this[int index]
    {
        get => _locations[index];
        set => _locations[index] = value;
    }

    public int Count => _locations.Count;

    public bool IsReadOnly => false;

    public void Add(Location item) => _locations.Add(item);

    public void Clear() => _locations.Clear();

    public bool Contains(Location item) => _locations.Contains(item);

    public void CopyTo(Location[] array, int arrayIndex) => _locations.CopyTo(array, arrayIndex);

    public IEnumerator<Location> GetEnumerator() => _locations.GetEnumerator();

    public int IndexOf(Location item) => _locations.IndexOf(item);

    public void Insert(int index, Location item) => _locations.Insert(index, item);

    public bool Remove(Location item) => _locations.Remove(item);

    public void RemoveAt(int index) => _locations.RemoveAt(index);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _locations.GetEnumerator();

    /// <summary>
    /// Creates a new instance of <see cref="Locations"/> by merging two collections of <see cref="Location"/> objects.
    /// </summary>
    /// <param name="jcLocations">A collection of <see cref="Location"/> objects representing JC locations.</param>
    /// <param name="pcssLocations">A collection of <see cref="Location"/> objects representing PCSS locations.</param>
    /// <returns>A new instance of <see cref="Locations"/> containing the merged locations.</returns>
    /// <remarks>
    /// This method merges the PCSS and JC locations based on matching <see cref="Location.Code"/> and <see cref="Location.LocationId"/>.
    /// Only those who have matching locations and are active will be included in the resulting collection.
    /// </remarks>
    public static Locations Create(ICollection<Location> jcLocations, ICollection<Location> pcssLocations)
    {
        var locations = pcssLocations
            .Select(pcss =>
            {
                var match = jcLocations.SingleOrDefault(jc => jc.LocationId == pcss.Code || jc.Code == pcss.Name);
                return match != null ? Location.Create(match, pcss) : null;
            })
            .Where(loc => loc != null)
            .OrderBy(loc => loc.ShortName)
            .ToList();

        return [.. locations];
    }
}