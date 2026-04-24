namespace Scv.Core.Helpers.Extensions;

public static class DictionaryExtensions
{
    public static string? GetValue(this Dictionary<string, string> dictionary, string key)
    {
        return dictionary.TryGetValue(key, out var value) ? value : null;
    }
}
