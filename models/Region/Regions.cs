namespace Scv.Models.Region;

public class Regions : IList<Region>
{
    private readonly List<Region> _regions;

    public Regions()
    {
        _regions = [];
    }

    public Regions(IEnumerable<Region> regions)
    {
        _regions = [.. regions];
    }

    public Region this[int index]
    {
        get => _regions[index];
        set => _regions[index] = value;
    }

    public int Count => _regions.Count;

    public bool IsReadOnly => false;

    public void Add(Region item) => _regions.Add(item);

    public void Clear() => _regions.Clear();

    public bool Contains(Region item) => _regions.Contains(item);

    public void CopyTo(Region[] array, int arrayIndex) => _regions.CopyTo(array, arrayIndex);

    public IEnumerator<Region> GetEnumerator() => _regions.GetEnumerator();

    public int IndexOf(Region item) => _regions.IndexOf(item);

    public void Insert(int index, Region item) => _regions.Insert(index, item);

    public bool Remove(Region item) => _regions.Remove(item);

    public void RemoveAt(int index) => _regions.RemoveAt(index);

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => _regions.GetEnumerator();

    /// <summary>
    /// Creates a new instance of <see cref="Regions"/> by merging two collections of <see cref="Region"/> objects.
    /// </summary>
    /// <param name="pcssRegions">A collection of <see cref="Region"/> objects representing PCSS locations.</param>
    /// <returns>A new instance of <see cref="Regions"/> containing the merged locations.</returns>
    /// <remarks>
    /// <see cref="Region.Code"/>.
    /// </remarks>
    public static Regions Create(ICollection<Region> pcssRegions)
    {
        var locations = pcssRegions
            .Where(loc => loc.Active.GetValueOrDefault())
            .OrderBy(loc => loc.Code)
            .ToList();

        return [.. locations];
    }
}