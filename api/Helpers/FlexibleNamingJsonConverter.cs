// Turn on nullable to prevent compiler warnings
#nullable enable

using System;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scv.Api.Helpers;

/// <summary>
/// Deserializes JSON into T regardless of property naming (snake_case, camelCase, PascalCase).
/// Works for nested objects and arrays. Read-only (no writing).
/// </summary>
public class FlexibleNamingJsonConverter<T> : JsonConverter where T : new()
{
    public override bool CanWrite => false;
    public override bool CanConvert(Type objectType) => objectType == typeof(T);

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var jObject = JObject.Load(reader);
        var target = new T();
        var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var prop in props)
        {
            if (!prop.CanWrite) continue;

            var candidates = new[]
            {
                ToSnakeCase(prop.Name),       // snake_case
                ToCamelCase(prop.Name),       // camelCase
                prop.Name                     // PascalCase
            };

            foreach (var name in candidates)
            {
                if (!jObject.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var token))
                {
                    continue;
                }

                var value = ConvertToken(token, prop.PropertyType, serializer);
                prop.SetValue(target, value);
                break;
            }
        }

        return target;
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        => throw new NotImplementedException("This converter only supports deserialization.");

    private static object? ConvertToken(JToken token, Type targetType, JsonSerializer serializer)
    {
        // If complex object and not a string, normalize its properties before converting
        if (token.Type == JTokenType.Object && targetType.IsClass && targetType != typeof(string))
        {
            var normalized = NormalizeObject((JObject)token, targetType);
            return normalized.ToObject(targetType, serializer);
        }

        // If an array of complex objects, normalize each element before converting
        if (token.Type == JTokenType.Array)
        {
            // Try to determine the element type of the target collection
            var elementType = GetElementType(targetType);
            if (elementType != null && elementType.IsClass && elementType != typeof(string))
            {
                var jArray = (JArray)token;
                var normalizedArray = new JArray();
                foreach (var item in jArray)
                {
                    normalizedArray.Add(item is JObject obj
                        ? NormalizeObject(obj, elementType)
                        : item);
                }
                return normalizedArray.ToObject(targetType, serializer);
            }
        }

        // Arrays of primitives or scalars: Direct conversion
        return token.ToObject(targetType, serializer);
    }

    private static Type? GetElementType(Type type)
    {
        // Handles List<T>, T[], ICollection<T>, IEnumerable<T>, etc.
        if (type.IsArray) return type.GetElementType();
        if (type.IsGenericType)
        {
            var args = type.GetGenericArguments();
            if (args.Length == 1) return args[0];
        }
        return null;
    }

    private static JObject NormalizeObject(JObject source, Type targetType)
    {
        var result = new JObject();
        var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var jProp in source.Properties())
        {
            var match = FindMatch(props, jProp.Name);
            if (match == null)
            {
                // Keep original if we can't map (extension/unknown props)
                result[jProp.Name] = jProp.Value;
                continue;
            }

            var camelName = ToCamelCase(match.Name);
            result[camelName] = NormalizeValue(jProp.Value, match.PropertyType);
        }

        return result;
    }

    private static JToken NormalizeValue(JToken value, Type targetPropertyType)
    {
        return value.Type switch
        {
            JTokenType.Object when targetPropertyType.IsClass && targetPropertyType != typeof(string)
                => NormalizeObject((JObject)value, targetPropertyType),

            JTokenType.Array
                => NormalizeArray((JArray)value, targetPropertyType),

            _ => value
        };
    }

    private static JToken NormalizeArray(JArray array, Type targetPropertyType)
    {
        var elementType = GetElementType(targetPropertyType);
        if (elementType == null || !elementType.IsClass || elementType == typeof(string))
        {
            return array; // primitives or unknown collections
        }

        var normalizedArr = new JArray();
        foreach (var item in array)
        {
            normalizedArr.Add(item is JObject obj ? NormalizeObject(obj, elementType) : item);
        }
        return normalizedArr;
    }

    private static PropertyInfo? FindMatch(PropertyInfo[] props, string jsonName)
    {
        // Match by exact, snake_case, or camelCase (case-insensitive)
        return props.FirstOrDefault(p =>
            p.Name.Equals(jsonName, StringComparison.OrdinalIgnoreCase) ||
            ToSnakeCase(p.Name).Equals(jsonName, StringComparison.OrdinalIgnoreCase) ||
            ToCamelCase(p.Name).Equals(jsonName, StringComparison.OrdinalIgnoreCase));
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return string.Concat(input.Select((c, i) => i > 0 && char.IsUpper(c) ? "_" + c : c.ToString())).ToLowerInvariant();
    }

    private static string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLowerInvariant(input[0]) + input.Substring(1);
    }
}
