using System.Collections;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TextForge.Core.Transformers;

public static class DeveloperTransformers
{
    public static string ToSqlInClause(string? text, bool multiLine = false, bool forceQuotes = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return "IN ()";

        var lines = ExtractItems(text);
        if (lines.Count == 0) return "IN ()";

        bool allNumbers = !forceQuotes && lines.All(l => double.TryParse(l, System.Globalization.CultureInfo.InvariantCulture, out _) || long.TryParse(l, out _));

        var formattedItems = lines.Select(item =>
        {
            if (allNumbers) return item;
            return $"'{item.Replace("'", "''")}'";
        }).ToList();

        if (multiLine)
        {
            var sb = new StringBuilder();
            sb.AppendLine("IN (");
            for (int i = 0; i < formattedItems.Count; i++)
            {
                string comma = i < formattedItems.Count - 1 ? "," : "";
                sb.AppendLine($"    {formattedItems[i]}{comma}");
            }
            sb.Append(")");
            return sb.ToString();
        }

        return $"IN ({string.Join(", ", formattedItems)})";
    }

    public static string ToCSharpArray(string? text, string variableName = "items", bool asList = false)
    {
        var items = ExtractItems(text);
        if (items.Count == 0) return asList ? $"var {variableName} = new List<string>();" : $"var {variableName} = Array.Empty<string>();";

        bool allInts = items.All(i => int.TryParse(i, out _));
        bool allDoubles = !allInts && items.All(i => double.TryParse(i, System.Globalization.CultureInfo.InvariantCulture, out _));

        string typeName = allInts ? "int" : allDoubles ? "double" : "string";
        var formatted = items.Select(i => typeName == "string" ? $"\"{i.Replace("\"", "\\\"")}\"" : i).ToList();

        if (asList)
        {
            return $"var {variableName} = new List<{typeName}>\n{{\n    {string.Join(",\n    ", formatted)}\n}};";
        }

        return $"var {variableName} = new {typeName}[]\n{{\n    {string.Join(",\n    ", formatted)}\n}};";
    }

    public static string ToTypeScriptArray(string? text, string variableName = "items")
    {
        var items = ExtractItems(text);
        bool allNumbers = items.Count > 0 && items.All(i => double.TryParse(i, System.Globalization.CultureInfo.InvariantCulture, out _));

        var formatted = items.Select(i => allNumbers ? i : $"\"{i.Replace("\"", "\\\"")}\"").ToList();
        return $"const {variableName} = [\n  {string.Join(",\n  ", formatted)}\n];";
    }

    public static string ToPythonList(string? text, string variableName = "items")
    {
        var items = ExtractItems(text);
        bool allNumbers = items.Count > 0 && items.All(i => double.TryParse(i, System.Globalization.CultureInfo.InvariantCulture, out _));

        var formatted = items.Select(i => allNumbers ? i : $"\"{i.Replace("\"", "\\\"")}\"").ToList();
        return $"{variableName} = [\n    {string.Join(",\n    ", formatted)}\n]";
    }

    public static string ToJsonArray(string? text, bool indented = true)
    {
        var items = ExtractItems(text);
        bool allInts = items.Count > 0 && items.All(i => long.TryParse(i, out _));
        bool allDoubles = !allInts && items.Count > 0 && items.All(i => double.TryParse(i, System.Globalization.CultureInfo.InvariantCulture, out _));

        object listObj;
        if (allInts)
        {
            listObj = items.Select(long.Parse).ToList();
        }
        else if (allDoubles)
        {
            listObj = items.Select(i => double.Parse(i, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        }
        else
        {
            listObj = items;
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(listObj, options);
    }

    public static string ToYamlArray(string? text)
    {
        var items = ExtractItems(text);
        if (items.Count == 0) return "[]";

        bool allInts = items.All(i => long.TryParse(i, out _));
        bool allDoubles = !allInts && items.All(i => double.TryParse(i, System.Globalization.CultureInfo.InvariantCulture, out _));
        bool allBools = !allInts && !allDoubles && items.All(i => bool.TryParse(i, out _));

        object listObj;
        if (allInts)
            listObj = items.Select(long.Parse).ToList();
        else if (allDoubles)
            listObj = items.Select(i => double.Parse(i, System.Globalization.CultureInfo.InvariantCulture)).ToList();
        else if (allBools)
            listObj = items.Select(bool.Parse).ToList();
        else
            listObj = items;

        var serializer = new SerializerBuilder().Build();
        return serializer.Serialize(listObj).TrimEnd('\r', '\n');
    }

    public static string QueryStringToKeyValuePairs(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        string query = text.Trim();
        if (query.StartsWith('?')) query = query[1..];

        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries);
        var sb = new StringBuilder();

        foreach (var pair in pairs)
        {
            int eqIdx = pair.IndexOf('=');
            if (eqIdx >= 0)
            {
                string key = Uri.UnescapeDataString(pair[..eqIdx]);
                string val = Uri.UnescapeDataString(pair[(eqIdx + 1)..]);
                sb.AppendLine($"{key}: {val}");
            }
            else
            {
                sb.AppendLine(Uri.UnescapeDataString(pair));
            }
        }

        return sb.ToString().TrimEnd('\r', '\n');
    }

    public static string KeyValuePairsToQueryString(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var pairs = new List<string>();

        foreach (var line in lines)
        {
            int idx = line.IndexOfAny(new[] { ':', '=' });
            if (idx >= 0)
            {
                string key = line[..idx].Trim();
                string val = line[(idx + 1)..].Trim();
                pairs.Add($"{Uri.EscapeDataString(key)}={Uri.EscapeDataString(val)}");
            }
            else
            {
                pairs.Add(Uri.EscapeDataString(line.Trim()));
            }
        }

        return string.Join("&", pairs);
    }

    public static string KeyValuePairsToJson(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var dict = new Dictionary<string, object?>();

        foreach (var line in lines)
        {
            int idx = line.IndexOfAny(new[] { ':', '=' });
            if (idx >= 0)
            {
                string key = line[..idx].Trim();
                string val = line[(idx + 1)..].Trim();

                if (long.TryParse(val, out long l))
                    dict[key] = l;
                else if (double.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out double d))
                    dict[key] = d;
                else if (bool.TryParse(val, out bool b))
                    dict[key] = b;
                else
                    dict[key] = val;
            }
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        return JsonSerializer.Serialize(dict, options);
    }

    public static string KeyValuePairsToYaml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var dict = new Dictionary<string, object?>();

        foreach (var line in lines)
        {
            int idx = line.IndexOfAny(new[] { ':', '=' });
            if (idx >= 0)
            {
                string key = line[..idx].Trim();
                string val = line[(idx + 1)..].Trim();

                if (long.TryParse(val, out long l))
                    dict[key] = l;
                else if (double.TryParse(val, System.Globalization.CultureInfo.InvariantCulture, out double d))
                    dict[key] = d;
                else if (bool.TryParse(val, out bool b))
                    dict[key] = b;
                else
                    dict[key] = val;
            }
        }

        var serializer = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .Build();

        return serializer.Serialize(dict).TrimEnd('\r', '\n');
    }

    public static string JsonToYaml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(text.Trim()));
            if (yamlObject == null) return string.Empty;

            var serializer = new SerializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            return serializer.Serialize(yamlObject).TrimEnd('\r', '\n');
        }
        catch
        {
            return text;
        }
    }

    public static string YamlToJson(string? text, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(text.Trim()));
            if (yamlObject == null) return "{}";

            var typedObject = ConvertYamlObjectToTyped(yamlObject);

            var options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            return JsonSerializer.Serialize(typedObject, options);
        }
        catch
        {
            return text;
        }
    }

    public static string XmlToJson(string? text, bool indented = true) => Structured.StructuredTransformers.XmlToJson(text, indented);
    public static string JsonToXml(string? text, string rootElementName = "root", bool indented = true) => Structured.StructuredTransformers.JsonToXml(text, rootElementName, indented);
    public static string XmlToYaml(string? text) => Structured.StructuredTransformers.XmlToYaml(text);
    public static string YamlToXml(string? text, string rootElementName = "root", bool indented = true) => Structured.StructuredTransformers.YamlToXml(text, rootElementName, indented);
    public static string MinifyJson(string? text) => Structured.StructuredTransformers.MinifyJson(text);
    public static string MinifyXml(string? text) => Structured.StructuredTransformers.MinifyXml(text);
    public static string FlattenStructured(string? text, string separator = ".") => Structured.StructuredTransformers.Flatten(text, separator);
    public static string FlattenToFlatJson(string? text, string separator = ".") => Structured.StructuredTransformers.FlattenToFlatJson(text, separator);
    public static string UnflattenStructured(string? text, string format = "JSON") => Structured.StructuredTransformers.Unflatten(text, format);
    public static string SortStructuredKeys(string? text, bool descending = false) => Structured.StructuredTransformers.SortKeys(text, descending);
    public static string ExtractStructuredPaths(string? text) => Structured.StructuredTransformers.ExtractPaths(text);
    public static string ExtractStructuredKeys(string? text) => Structured.StructuredTransformers.ExtractKeys(text);
    public static string ExtractStructuredValues(string? text) => Structured.StructuredTransformers.ExtractValues(text);
    public static string ConvertStructuredKeysCase(string? text, TextCasing casing) => Structured.StructuredTransformers.ConvertKeysCase(text, casing);
    public static string PickStructuredKeys(string? text, string? keyList) => Structured.StructuredTransformers.PickKeys(text, keyList);
    public static string OmitStructuredKeys(string? text, string? keyList) => Structured.StructuredTransformers.OmitKeys(text, keyList);
    public static string RemoveNullsAndEmpty(string? text) => Structured.StructuredTransformers.RemoveNullsAndEmpty(text);
    public static string QueryStructuredPath(string? text, string? query) => Structured.StructuredTransformers.QueryPath(text, query);
    public static string StructuredToCsv(string? text, char delimiter = ',') => Structured.StructuredTransformers.ToCsv(text, delimiter);
    public static string StructuredToTsv(string? text) => Structured.StructuredTransformers.ToTsv(text);
    public static string StructuredToMarkdown(string? text) => Structured.StructuredTransformers.ToMarkdownTable(text);
    public static string ToTypeScriptInterfaces(string? text, string rootName = "Root") => Structured.StructuredTransformers.ToTypeScriptInterfaces(text, rootName);
    public static string ToCSharpClasses(string? text, string rootName = "Root") => Structured.StructuredTransformers.ToCSharpClasses(text, rootName);
    public static string ToJsonSchema(string? text, string title = "Schema") => Structured.StructuredTransformers.ToJsonSchema(text, title);

    private static object? ConvertYamlObjectToTyped(object? obj)
    {
        if (obj is null) return null;

        if (obj is IDictionary dict)
        {
            var result = new Dictionary<string, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                result[key] = ConvertYamlObjectToTyped(entry.Value);
            }
            return result;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                result.Add(ConvertYamlObjectToTyped(item));
            }
            return result;
        }

        if (obj is string str)
        {
            if (long.TryParse(str, out long l)) return l;
            if (double.TryParse(str, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)) return d;
            if (bool.TryParse(str, out bool b)) return b;
            return str;
        }

        return obj;
    }

    private static List<string> ExtractItems(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return new List<string>();

        var rawLines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (rawLines.Length == 1 && (rawLines[0].Contains(',') || rawLines[0].Contains('\t') || rawLines[0].Contains(';')))
        {
            // Split single line
            char delim = rawLines[0].Contains('\t') ? '\t' : rawLines[0].Contains(';') ? ';' : ',';
            return rawLines[0].Split(delim)
                              .Select(s => s.Trim().Trim('\'', '"'))
                              .Where(s => !string.IsNullOrEmpty(s))
                              .ToList();
        }

        return rawLines.Select(s => s.Trim().Trim('\'', '"', ','))
                       .Where(s => !string.IsNullOrEmpty(s))
                       .ToList();
    }
}
