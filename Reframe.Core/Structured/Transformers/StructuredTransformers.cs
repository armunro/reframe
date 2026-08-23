using System.Collections;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using Reframe.Core.Tabular;
using Reframe.Core.Transformers;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Reframe.Core.Structured;

public static class StructuredTransformers
{
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions CompactJsonOptions = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ==========================================
    // 1. FORMAT CONVERSIONS (XML, JSON, YAML)
    // ==========================================

    public static string XmlToJson(string? xml, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(xml)) return "{}";

        try
        {
            var doc = XDocument.Parse(xml.Trim());
            if (doc.Root == null) return "{}";

            var rootObj = ConvertXmlElementToObject(doc.Root);
            var wrapper = new Dictionary<string, object?>
            {
                [doc.Root.Name.LocalName] = rootObj
            };

            var options = indented ? IndentedJsonOptions : CompactJsonOptions;
            return JsonSerializer.Serialize(wrapper, options);
        }
        catch (Exception ex)
        {
            return $"Error converting XML to JSON: {ex.Message}";
        }
    }

    private static object? ConvertXmlElementToObject(XElement element)
    {
        var dict = new Dictionary<string, object?>();

        // Attributes
        foreach (var attr in element.Attributes())
        {
            dict["@" + attr.Name.LocalName] = ParseScalarValue(attr.Value);
        }

        // Child Elements
        var childGroups = element.Elements().GroupBy(e => e.Name.LocalName).ToList();

        if (childGroups.Count == 0 && !element.HasAttributes)
        {
            return ParseScalarValue(element.Value);
        }

        foreach (var group in childGroups)
        {
            if (group.Count() > 1)
            {
                var list = group.Select(ConvertXmlElementToObject).ToList();
                dict[group.Key] = list;
            }
            else
            {
                var child = group.First();
                dict[group.Key] = ConvertXmlElementToObject(child);
            }
        }

        if (!element.HasElements && !string.IsNullOrEmpty(element.Value) && element.HasAttributes)
        {
            dict["#text"] = ParseScalarValue(element.Value);
        }

        return dict;
    }

    public static string JsonToXml(string? json, string rootElementName = "root", bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(json)) return $"<{rootElementName}/>";

        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            var rootElement = new XElement(SanitizeXmlName(rootElementName));

            PopulateXmlElementFromJson(rootElement, doc.RootElement);

            // If root element has a single child object and root was a generic wrapper, we can present it nicely
            var settings = new XmlWriterSettings
            {
                Indent = indented,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                OmitXmlDeclaration = false
            };

            var xdoc = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), rootElement);
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, settings))
            {
                xdoc.Save(xw);
            }
            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error converting JSON to XML: {ex.Message}";
        }
    }

    private static void PopulateXmlElementFromJson(XElement parent, JsonElement json)
    {
        switch (json.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in json.EnumerateObject())
                {
                    string propName = prop.Name;
                    if (propName.StartsWith('@') && propName.Length > 1)
                    {
                        parent.SetAttributeValue(SanitizeXmlName(propName[1..]), prop.Value.ToString());
                    }
                    else if (propName == "#text")
                    {
                        parent.Value = prop.Value.GetString() ?? string.Empty;
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        string itemName = SanitizeXmlName(propName);
                        foreach (var item in prop.Value.EnumerateArray())
                        {
                            var childElem = new XElement(itemName);
                            PopulateXmlElementFromJson(childElem, item);
                            parent.Add(childElem);
                        }
                    }
                    else
                    {
                        var childElem = new XElement(SanitizeXmlName(propName));
                        PopulateXmlElementFromJson(childElem, prop.Value);
                        parent.Add(childElem);
                    }
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in json.EnumerateArray())
                {
                    var itemElem = new XElement("item");
                    PopulateXmlElementFromJson(itemElem, item);
                    parent.Add(itemElem);
                }
                break;

            case JsonValueKind.String:
                parent.Value = json.GetString() ?? string.Empty;
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                break;

            default:
                parent.Value = json.GetRawText();
                break;
        }
    }

    public static string XmlToYaml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return string.Empty;

        try
        {
            var doc = XDocument.Parse(xml.Trim());
            if (doc.Root == null) return string.Empty;

            var rootObj = ConvertXmlElementToObject(doc.Root);
            var wrapper = new Dictionary<string, object?>
            {
                [doc.Root.Name.LocalName] = rootObj
            };

            var serializer = new SerializerBuilder()
                .WithNamingConvention(NullNamingConvention.Instance)
                .Build();

            return serializer.Serialize(wrapper).TrimEnd('\r', '\n');
        }
        catch (Exception ex)
        {
            return $"Error converting XML to YAML: {ex.Message}";
        }
    }

    public static string YamlToXml(string? yaml, string rootElementName = "root", bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(yaml)) return $"<{rootElementName}/>";

        try
        {
            string json = DeveloperTransformers.YamlToJson(yaml, false);
            return JsonToXml(json, rootElementName, indented);
        }
        catch (Exception ex)
        {
            return $"Error converting YAML to XML: {ex.Message}";
        }
    }

    public static string JsonToYaml(string? json)
    {
        return DeveloperTransformers.JsonToYaml(json);
    }

    public static string YamlToJson(string? yaml, bool indented = true)
    {
        return DeveloperTransformers.YamlToJson(yaml, indented);
    }

    // ==========================================
    // 2. MINIFICATION & BEAUTIFICATION
    // ==========================================

    public static string MinifyJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = JsonDocument.Parse(json.Trim());
            return JsonSerializer.Serialize(doc.RootElement, CompactJsonOptions);
        }
        catch
        {
            return json;
        }
    }

    public static string MinifyXml(string? xml)
    {
        if (string.IsNullOrWhiteSpace(xml)) return string.Empty;
        try
        {
            var doc = XDocument.Parse(xml.Trim());
            var settings = new XmlWriterSettings
            {
                Indent = false,
                NewLineChars = "",
                OmitXmlDeclaration = doc.Declaration == null
            };
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, settings))
            {
                doc.Save(xw);
            }
            return sb.ToString().Trim();
        }
        catch
        {
            return xml;
        }
    }

    public static string Beautify(string? text)
    {
        return TextBeautifier.Beautify(text);
    }

    public static string BeautifyJson(string? json)
    {
        return TextBeautifier.BeautifyJson(json, true);
    }

    public static string BeautifyXml(string? xml)
    {
        return TextBeautifier.BeautifyXml(xml);
    }

    public static string BeautifyYaml(string? yaml)
    {
        return TextBeautifier.BeautifyYaml(yaml);
    }

    // ==========================================
    // 3. FLATTEN & UNFLATTEN
    // ==========================================

    public static string Flatten(string? text, string separator = ".")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var parseResult = StructuredDataParser.Parse(text);
        if (!parseResult.Success || parseResult.RootNodes.Count == 0)
        {
            return text;
        }

        var lines = new List<string>();
        foreach (var root in parseResult.RootNodes)
        {
            CollectFlattenedLines(root, string.Empty, separator, lines);
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static void CollectFlattenedLines(StructuredDataNode node, string currentPrefix, string separator, List<string> lines)
    {
        string nodeName = node.Name;
        if (nodeName.StartsWith('<') && nodeName.EndsWith('>'))
            nodeName = nodeName[1..^1];

        string fullKey;
        if (string.IsNullOrEmpty(currentPrefix) || currentPrefix == "$")
        {
            fullKey = nodeName == "$" ? "" : nodeName;
        }
        else if (nodeName.StartsWith('['))
        {
            fullKey = $"{currentPrefix}{nodeName}";
        }
        else
        {
            fullKey = $"{currentPrefix}{separator}{nodeName}";
        }

        if (node.Children.Count == 0)
        {
            string val = node.Value != null ? FormatScalarForDisplay(node.Value, node.NodeType) : "null";
            if (!string.IsNullOrEmpty(fullKey))
            {
                lines.Add($"{fullKey} = {val}");
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                CollectFlattenedLines(child, fullKey, separator, lines);
            }
        }
    }

    public static string Unflatten(string? text, string format = "JSON")
    {
        if (string.IsNullOrWhiteSpace(text)) return format.Equals("YAML", StringComparison.OrdinalIgnoreCase) ? "{}" : "{}";

        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        var root = new JsonObject();

        foreach (var line in lines)
        {
            int eqIdx = line.IndexOfAny(new[] { '=', ':' });
            if (eqIdx <= 0) continue;

            string keyPath = line[..eqIdx].Trim();
            string valStr = line[(eqIdx + 1)..].Trim();

            SetDeepJsonValue(root, keyPath, ParseScalarValue(valStr));
        }

        if (format.Equals("YAML", StringComparison.OrdinalIgnoreCase) || format.Equals("YML", StringComparison.OrdinalIgnoreCase))
        {
            return DeveloperTransformers.JsonToYaml(root.ToJsonString(IndentedJsonOptions));
        }

        return root.ToJsonString(IndentedJsonOptions);
    }

    private static void SetDeepJsonValue(JsonObject root, string path, object? value)
    {
        var segments = ParsePathSegments(path);
        if (segments.Count == 0) return;

        JsonNode current = root;

        for (int i = 0; i < segments.Count - 1; i++)
        {
            string seg = segments[i];
            string nextSeg = segments[i + 1];
            bool nextIsIndex = int.TryParse(nextSeg, out int _);

            if (current is JsonObject curObj)
            {
                if (!curObj.ContainsKey(seg) || curObj[seg] == null)
                {
                    curObj[seg] = nextIsIndex ? new JsonArray() : new JsonObject();
                }
                current = curObj[seg]!;
            }
            else if (current is JsonArray curArr)
            {
                if (int.TryParse(seg, out int arrIdx))
                {
                    while (curArr.Count <= arrIdx)
                    {
                        curArr.Add(null);
                    }
                    if (curArr[arrIdx] == null)
                    {
                        curArr[arrIdx] = nextIsIndex ? new JsonArray() : new JsonObject();
                    }
                    current = curArr[arrIdx]!;
                }
            }
        }

        string lastSeg = segments[^1];
        var jsonVal = ConvertToJsonNode(value);

        if (current is JsonObject finalObj)
        {
            finalObj[lastSeg] = jsonVal;
        }
        else if (current is JsonArray finalArr)
        {
            if (int.TryParse(lastSeg, out int arrIdx))
            {
                while (finalArr.Count <= arrIdx)
                {
                    finalArr.Add(null);
                }
                finalArr[arrIdx] = jsonVal;
            }
        }
    }

    private static List<string> ParsePathSegments(string path)
    {
        var segments = new List<string>();
        var sb = new StringBuilder();

        for (int i = 0; i < path.Length; i++)
        {
            char c = path[i];
            if (c == '.' || c == '/')
            {
                if (sb.Length > 0)
                {
                    segments.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (c == '[')
            {
                if (sb.Length > 0)
                {
                    segments.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (c == ']')
            {
                if (sb.Length > 0)
                {
                    segments.Add(sb.ToString());
                    sb.Clear();
                }
            }
            else if (c == '$')
            {
                // Ignore leading $
            }
            else
            {
                sb.Append(c);
            }
        }

        if (sb.Length > 0)
        {
            segments.Add(sb.ToString());
        }

        return segments;
    }

    private static JsonNode? ConvertToJsonNode(object? obj)
    {
        if (obj == null) return null;
        if (obj is string s) return JsonValue.Create(s);
        if (obj is long l) return JsonValue.Create(l);
        if (obj is int i) return JsonValue.Create(i);
        if (obj is double d) return JsonValue.Create(d);
        if (obj is bool b) return JsonValue.Create(b);
        return JsonValue.Create(obj.ToString());
    }

    // ==========================================
    // 4. KEYS / PATHS EXTRACTION & SORTING
    // ==========================================

    public static string ExtractPaths(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var parseResult = StructuredDataParser.Parse(text);
        if (!parseResult.Success) return string.Empty;

        var paths = new List<string>();
        foreach (var root in parseResult.RootNodes)
        {
            CollectAllPaths(root, paths);
        }

        return string.Join(Environment.NewLine, paths.Distinct());
    }

    private static void CollectAllPaths(StructuredDataNode node, List<string> paths)
    {
        if (!string.IsNullOrEmpty(node.Path) && node.Path != "$")
        {
            paths.Add(node.Path);
        }

        foreach (var child in node.Children)
        {
            CollectAllPaths(child, paths);
        }
    }

    public static string ExtractKeys(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var parseResult = StructuredDataParser.Parse(text);
        if (!parseResult.Success) return string.Empty;

        var keys = new List<string>();
        foreach (var root in parseResult.RootNodes)
        {
            CollectLeafKeys(root, keys);
        }

        return string.Join(Environment.NewLine, keys.Distinct());
    }

    private static void CollectLeafKeys(StructuredDataNode node, List<string> keys)
    {
        if (!node.HasChildren && !string.IsNullOrEmpty(node.Name) && !node.Name.StartsWith('['))
        {
            keys.Add(node.Name.TrimStart('@', '<').TrimEnd('>'));
        }

        foreach (var child in node.Children)
        {
            CollectLeafKeys(child, keys);
        }
    }

    public static string SortKeys(string? text, bool descending = false)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string trimmed = text.Trim();

        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var sorted = SortJsonElement(doc.RootElement, descending);
                return JsonSerializer.Serialize(sorted, IndentedJsonOptions);
            }
            catch
            {
                return text;
            }
        }

        if (TextBeautifier.IsYaml(trimmed))
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(trimmed));
                var sorted = SortYamlObject(yamlObj, descending);
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                return serializer.Serialize(sorted).TrimEnd('\r', '\n');
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    private static object? SortJsonElement(JsonElement element, bool descending = false)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var comparer = descending
                    ? Comparer<string>.Create((a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase))
                    : (IComparer<string>)StringComparer.OrdinalIgnoreCase;
                var sortedDict = new SortedDictionary<string, object?>(comparer);
                foreach (var prop in element.EnumerateObject())
                {
                    sortedDict[prop.Name] = SortJsonElement(prop.Value, descending);
                }
                return sortedDict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(SortJsonElement(item, descending));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l)) return l;
                if (element.TryGetDouble(out double d)) return d;
                return element.GetRawText();

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;

            default:
                return element.GetRawText();
        }
    }

    private static object? SortYamlObject(object? obj, bool descending = false)
    {
        if (obj == null) return null;

        if (obj is IDictionary dict)
        {
            var comparer = descending
                ? Comparer<string>.Create((a, b) => string.Compare(b, a, StringComparison.OrdinalIgnoreCase))
                : (IComparer<string>)StringComparer.OrdinalIgnoreCase;
            var sortedDict = new SortedDictionary<string, object?>(comparer);
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                sortedDict[key] = SortYamlObject(entry.Value, descending);
            }
            return sortedDict;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                result.Add(SortYamlObject(item, descending));
            }
            return result;
        }

        return obj;
    }

    // ==========================================
    // 5. KEY CASING & RENAMING
    // ==========================================

    public static string ConvertKeysCase(string? text, TextCasing casing)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string trimmed = text.Trim();

        // 1. JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var converted = ConvertJsonKeysCase(doc.RootElement, casing);
                return JsonSerializer.Serialize(converted, IndentedJsonOptions);
            }
            catch
            {
                // Fallback
            }
        }

        // 2. XML
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                var doc = XDocument.Parse(trimmed);
                ConvertXmlKeysCase(doc.Root, casing);
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = doc.Declaration == null
                };
                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(sb, settings))
                {
                    doc.Save(xw);
                }
                return sb.ToString().Trim();
            }
            catch
            {
                // Fallback
            }
        }

        // 3. YAML
        if (TextBeautifier.IsYaml(trimmed))
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(trimmed));
                var converted = ConvertYamlKeysCase(yamlObj, casing);
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                return serializer.Serialize(converted).TrimEnd('\r', '\n');
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    private static object? ConvertJsonKeysCase(JsonElement element, TextCasing casing)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    string convertedKey = CaseTransformers.ChangeCase(prop.Name, casing, false);
                    dict[convertedKey] = ConvertJsonKeysCase(prop.Value, casing);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonKeysCase(item, casing));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l)) return l;
                if (element.TryGetDouble(out double d)) return d;
                return element.GetRawText();

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;

            default:
                return element.GetRawText();
        }
    }

    private static object? ConvertYamlKeysCase(object? obj, TextCasing casing)
    {
        if (obj == null) return null;

        if (obj is IDictionary dict)
        {
            var result = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                string newKey = CaseTransformers.ChangeCase(key, casing, false);
                result[newKey] = ConvertYamlKeysCase(entry.Value, casing);
            }
            return result;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                result.Add(ConvertYamlKeysCase(item, casing));
            }
            return result;
        }

        return obj;
    }

    private static void ConvertXmlKeysCase(XElement? element, TextCasing casing)
    {
        if (element == null) return;

        string newName = CaseTransformers.ChangeCase(element.Name.LocalName, casing, false);
        element.Name = SanitizeXmlName(newName);

        foreach (var attr in element.Attributes().ToList())
        {
            string newAttrName = CaseTransformers.ChangeCase(attr.Name.LocalName, casing, false);
            attr.Remove();
            element.SetAttributeValue(SanitizeXmlName(newAttrName), attr.Value);
        }

        foreach (var child in element.Elements())
        {
            ConvertXmlKeysCase(child, casing);
        }
    }

    // ==========================================
    // 6. KEY FILTERING (PICK / OMIT / CLEAN)
    // ==========================================

    public static string PickKeys(string? text, string? keyList)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var keys = ParseKeyFilterList(keyList);
        if (keys.Count == 0) return text;

        string trimmed = text.Trim();

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var filtered = PickJsonKeys(doc.RootElement, keys);
                return JsonSerializer.Serialize(filtered, IndentedJsonOptions);
            }
            catch
            {
                // Fallback
            }
        }

        if (TextBeautifier.IsYaml(trimmed))
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(trimmed));
                var filtered = PickYamlKeys(yamlObj, keys);
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                return serializer.Serialize(filtered).TrimEnd('\r', '\n');
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    public static string OmitKeys(string? text, string? keyList)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        var keys = ParseKeyFilterList(keyList);
        if (keys.Count == 0) return text;

        string trimmed = text.Trim();

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var filtered = OmitJsonKeys(doc.RootElement, keys);
                return JsonSerializer.Serialize(filtered, IndentedJsonOptions);
            }
            catch
            {
                // Fallback
            }
        }

        if (TextBeautifier.IsYaml(trimmed))
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(trimmed));
                var filtered = OmitYamlKeys(yamlObj, keys);
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                return serializer.Serialize(filtered).TrimEnd('\r', '\n');
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    private static HashSet<string> ParseKeyFilterList(string? keyList)
    {
        if (string.IsNullOrWhiteSpace(keyList)) return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        return keyList.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                      .Select(k => k.Trim())
                      .Where(k => !string.IsNullOrEmpty(k))
                      .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static object? PickJsonKeys(JsonElement element, HashSet<string> keys)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    if (keys.Contains(prop.Name))
                    {
                        dict[prop.Name] = ConvertJsonElement(prop.Value);
                    }
                    else if (prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var nested = PickJsonKeys(prop.Value, keys);
                        if (nested is IDictionary d && d.Count > 0)
                            dict[prop.Name] = nested;
                        else if (nested is IList l && l.Count > 0)
                            dict[prop.Name] = nested;
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    var filteredItem = PickJsonKeys(item, keys);
                    if (filteredItem != null)
                        list.Add(filteredItem);
                }
                return list;

            default:
                return ConvertJsonElement(element);
        }
    }

    private static object? OmitJsonKeys(JsonElement element, HashSet<string> keys)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    if (!keys.Contains(prop.Name))
                    {
                        dict[prop.Name] = OmitJsonKeys(prop.Value, keys);
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(OmitJsonKeys(item, keys));
                }
                return list;

            default:
                return ConvertJsonElement(element);
        }
    }

    private static object? PickYamlKeys(object? obj, HashSet<string> keys)
    {
        if (obj == null) return null;

        if (obj is IDictionary dict)
        {
            var result = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                if (keys.Contains(key))
                {
                    result[entry.Key!] = entry.Value;
                }
                else if (entry.Value is IDictionary or IList)
                {
                    var nested = PickYamlKeys(entry.Value, keys);
                    if (nested is IDictionary d && d.Count > 0)
                        result[entry.Key!] = nested;
                    else if (nested is IList l && l.Count > 0)
                        result[entry.Key!] = nested;
                }
            }
            return result;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                var filtered = PickYamlKeys(item, keys);
                if (filtered != null)
                    result.Add(filtered);
            }
            return result;
        }

        return obj;
    }

    private static object? OmitYamlKeys(object? obj, HashSet<string> keys)
    {
        if (obj == null) return null;

        if (obj is IDictionary dict)
        {
            var result = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                if (!keys.Contains(key))
                {
                    result[entry.Key!] = OmitYamlKeys(entry.Value, keys);
                }
            }
            return result;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                result.Add(OmitYamlKeys(item, keys));
            }
            return result;
        }

        return obj;
    }

    public static string RemoveNullsAndEmpty(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string trimmed = text.Trim();

        // 1. JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                var cleaned = CleanJsonElement(doc.RootElement);
                return JsonSerializer.Serialize(cleaned ?? new Dictionary<string, object?>(), IndentedJsonOptions);
            }
            catch
            {
                // Fallback
            }
        }

        // 2. YAML
        if (TextBeautifier.IsYaml(trimmed))
        {
            try
            {
                var deserializer = new DeserializerBuilder().Build();
                var yamlObj = deserializer.Deserialize(new StringReader(trimmed));
                var cleaned = CleanYamlObject(yamlObj);
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(NullNamingConvention.Instance)
                    .Build();
                return serializer.Serialize(cleaned ?? new Dictionary<object, object?>()).TrimEnd('\r', '\n');
            }
            catch
            {
                return text;
            }
        }

        // 3. XML
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                var doc = XDocument.Parse(trimmed);
                CleanXmlElement(doc.Root);
                var settings = new XmlWriterSettings
                {
                    Indent = true,
                    IndentChars = "  ",
                    OmitXmlDeclaration = doc.Declaration == null
                };
                var sb = new StringBuilder();
                using (var xw = XmlWriter.Create(sb, settings))
                {
                    doc.Save(xw);
                }
                return sb.ToString().Trim();
            }
            catch
            {
                return text;
            }
        }

        return text;
    }

    private static object? CleanJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    var cleanedVal = CleanJsonElement(prop.Value);
                    if (cleanedVal != null)
                    {
                        if (cleanedVal is string s && string.IsNullOrEmpty(s)) continue;
                        if (cleanedVal is IDictionary d && d.Count == 0) continue;
                        if (cleanedVal is IList l && l.Count == 0) continue;
                        dict[prop.Name] = cleanedVal;
                    }
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    var cleanedItem = CleanJsonElement(item);
                    if (cleanedItem != null)
                    {
                        if (cleanedItem is string s && string.IsNullOrEmpty(s)) continue;
                        if (cleanedItem is IDictionary d && d.Count == 0) continue;
                        if (cleanedItem is IList l && l.Count == 0) continue;
                        list.Add(cleanedItem);
                    }
                }
                return list;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return null;

            case JsonValueKind.String:
                string strVal = element.GetString() ?? "";
                return string.IsNullOrEmpty(strVal) ? null : strVal;

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long lg)) return lg;
                if (element.TryGetDouble(out double db)) return db;
                return element.GetRawText();

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;

            default:
                return element.GetRawText();
        }
    }

    private static object? CleanYamlObject(object? obj)
    {
        if (obj == null) return null;

        if (obj is string s)
        {
            return string.IsNullOrEmpty(s) ? null : s;
        }

        if (obj is IDictionary dict)
        {
            var result = new Dictionary<object, object?>();
            foreach (DictionaryEntry entry in dict)
            {
                var cleanedVal = CleanYamlObject(entry.Value);
                if (cleanedVal != null)
                {
                    if (cleanedVal is string str && string.IsNullOrEmpty(str)) continue;
                    if (cleanedVal is IDictionary d && d.Count == 0) continue;
                    if (cleanedVal is IList l && l.Count == 0) continue;
                    result[entry.Key!] = cleanedVal;
                }
            }
            return result;
        }

        if (obj is IList list)
        {
            var result = new List<object?>();
            foreach (var item in list)
            {
                var cleanedItem = CleanYamlObject(item);
                if (cleanedItem != null)
                {
                    if (cleanedItem is string str && string.IsNullOrEmpty(str)) continue;
                    if (cleanedItem is IDictionary d && d.Count == 0) continue;
                    if (cleanedItem is IList l && l.Count == 0) continue;
                    result.Add(cleanedItem);
                }
            }
            return result;
        }

        return obj;
    }

    private static void CleanXmlElement(XElement? element)
    {
        if (element == null) return;

        foreach (var child in element.Elements().ToList())
        {
            CleanXmlElement(child);
            if (!child.HasElements && !child.HasAttributes && string.IsNullOrWhiteSpace(child.Value))
            {
                child.Remove();
            }
        }
    }

    // ==========================================
    // 7. FLATTEN TO FLAT JSON & EXTRACTION
    // ==========================================

    public static string FlattenToFlatJson(string? text, string separator = ".")
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";

        var parseResult = StructuredDataParser.Parse(text);
        if (!parseResult.Success || parseResult.RootNodes.Count == 0)
        {
            return text;
        }

        var lines = new List<string>();
        foreach (var root in parseResult.RootNodes)
        {
            CollectFlattenedLines(root, string.Empty, separator, lines);
        }

        var flatDict = new Dictionary<string, object?>();
        foreach (var line in lines)
        {
            int eqIdx = line.IndexOf('=');
            if (eqIdx > 0)
            {
                string key = line[..eqIdx].Trim();
                string valStr = line[(eqIdx + 1)..].Trim();
                flatDict[key] = ParseScalarValue(valStr);
            }
        }

        return JsonSerializer.Serialize(flatDict, IndentedJsonOptions);
    }

    public static string ExtractValues(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        var parseResult = StructuredDataParser.Parse(text);
        if (!parseResult.Success) return string.Empty;

        var values = new List<string>();
        foreach (var root in parseResult.RootNodes)
        {
            CollectLeafValues(root, values);
        }

        return string.Join(Environment.NewLine, values);
    }

    private static void CollectLeafValues(StructuredDataNode node, List<string> values)
    {
        if (!node.HasChildren && node.Value != null)
        {
            values.Add(node.Value);
        }

        foreach (var child in node.Children)
        {
            CollectLeafValues(child, values);
        }
    }

    public static string QueryPath(string? text, string? queryPath)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (string.IsNullOrWhiteSpace(queryPath)) return text;

        string query = queryPath.Trim();
        string trimmed = text.Trim();

        // 1. Check if query is XPath syntax (starts with /, //, ./, @, *, contains /* or contains XPath functions/predicates or XML input)
        bool isXPathQuery = query.StartsWith('/') ||
                            query.StartsWith("./") ||
                            query.StartsWith('@') ||
                            query.StartsWith('*') ||
                            query.StartsWith("count(") ||
                            query.StartsWith("sum(") ||
                            query.StartsWith("string(") ||
                            query.StartsWith("boolean(") ||
                            query.StartsWith("name(") ||
                            query.StartsWith("local-name(") ||
                            query.Contains("/*") ||
                            query.Contains("//") ||
                            (trimmed.StartsWith('<') && trimmed.EndsWith('>'));

        if (isXPathQuery)
        {
            try
            {
                string xpathResult = QueryXPath(text, query);
                if (!xpathResult.StartsWith("No results matching") &&
                    !xpathResult.StartsWith("XPath error") &&
                    !xpathResult.StartsWith("Error"))
                {
                    return xpathResult;
                }
            }
            catch
            {
                // Fallback to structured node / JSONPath search
            }
        }

        // 2. StructuredDataParser search for node path or name or JSONPath
        var parseResult = StructuredDataParser.Parse(text);
        if (parseResult.Success)
        {
            var matchingNodes = new List<StructuredDataNode>();
            foreach (var root in parseResult.RootNodes)
            {
                FindMatchingQueryNodes(root, query, matchingNodes);
            }

            if (matchingNodes.Count == 1)
            {
                var single = matchingNodes[0];
                if (!single.HasChildren)
                    return single.Value ?? "null";
            }

            if (matchingNodes.Count > 0)
            {
                var results = matchingNodes.Select(n => n.HasChildren ? $"{n.Name}: {n.DisplayValue}" : $"{n.Name} = {n.Value}");
                return string.Join(Environment.NewLine, results);
            }
        }

        // 3. If QueryXPath hadn't been tried or returned no results, give clear feedback
        if (isXPathQuery)
        {
            return QueryXPath(text, query);
        }

        return $"No results matching query '{queryPath}'";
    }

    /// <summary>
    /// Executes an XPath query on XML, JSON, or YAML structured data, supporting full XPath 1.0 syntax,
    /// wildcards (*, //*, //@*, /*/*), predicates, attribute selections, and functions (count, sum, string).
    /// </summary>
    public static string QueryXPath(string? text, string? xpathQuery)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        if (string.IsNullOrWhiteSpace(xpathQuery)) return text;

        try
        {
            var results = EvaluateXPathInternal(text, xpathQuery.Trim());
            if (results.Count == 0)
            {
                return $"No results matching XPath '{xpathQuery}'";
            }

            return string.Join(Environment.NewLine, results);
        }
        catch (XPathException ex)
        {
            return $"XPath error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error querying XPath: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts inner text and scalar values of nodes matching an XPath query across structured data.
    /// </summary>
    public static string ExtractXPathValues(string? text, string? xpathQuery)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string query = string.IsNullOrWhiteSpace(xpathQuery) ? "//*[not(*)]/text() | //@*" : xpathQuery.Trim();

        try
        {
            var doc = GetXDocumentFromStructured(text);
            if (doc == null) return "Error: Unable to parse structured data for XPath.";

            var results = EvaluateXPathValuesInternal(doc, query);
            if (results.Count == 0)
            {
                var strippedDoc = RemoveNamespaces(doc);
                results = EvaluateXPathValuesInternal(strippedDoc, query);
            }

            if (results.Count == 0)
            {
                return $"No results matching XPath '{query}'";
            }

            return string.Join(Environment.NewLine, results);
        }
        catch (XPathException ex)
        {
            return $"XPath error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error extracting XPath values: {ex.Message}";
        }
    }

    /// <summary>
    /// Extracts attribute names and values matching an XPath query across structured data.
    /// </summary>
    public static string ExtractXPathAttributes(string? text, string? xpathQuery = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string query = string.IsNullOrWhiteSpace(xpathQuery) ? "//@*" : xpathQuery.Trim();

        try
        {
            var doc = GetXDocumentFromStructured(text);
            if (doc == null) return "Error: Unable to parse structured data for XPath.";

            var results = EvaluateXPathAttributesInternal(doc, query);
            if (results.Count == 0)
            {
                var strippedDoc = RemoveNamespaces(doc);
                results = EvaluateXPathAttributesInternal(strippedDoc, query);
            }

            if (results.Count == 0)
            {
                return $"No attributes matching XPath '{query}'";
            }

            return string.Join(Environment.NewLine, results);
        }
        catch (XPathException ex)
        {
            return $"XPath error: {ex.Message}";
        }
        catch (Exception ex)
        {
            return $"Error extracting XPath attributes: {ex.Message}";
        }
    }

    private static XDocument? GetXDocumentFromStructured(string text)
    {
        string trimmed = text.Trim();
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            return XDocument.Parse(trimmed);
        }

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            string xml = JsonToXml(trimmed, "root", indented: true);
            return XDocument.Parse(xml);
        }

        if (TextBeautifier.IsYaml(trimmed))
        {
            string xml = YamlToXml(trimmed, "root", indented: true);
            return XDocument.Parse(xml);
        }

        try
        {
            return XDocument.Parse(trimmed);
        }
        catch
        {
            try
            {
                string xml = JsonToXml(trimmed, "root", indented: true);
                return XDocument.Parse(xml);
            }
            catch
            {
                string xml = YamlToXml(trimmed, "root", indented: true);
                return XDocument.Parse(xml);
            }
        }
    }

    private static List<string> EvaluateXPathInternal(string text, string xpathQuery)
    {
        var doc = GetXDocumentFromStructured(text);
        if (doc == null) return new List<string>();

        var list = EvaluateXPathNodes(doc, xpathQuery);
        if (list.Count == 0)
        {
            var strippedDoc = RemoveNamespaces(doc);
            list = EvaluateXPathNodes(strippedDoc, xpathQuery);
        }

        return list;
    }

    private static List<string> EvaluateXPathNodes(XDocument doc, string xpathQuery)
    {
        var results = new List<string>();
        object evaluation = doc.XPathEvaluate(xpathQuery);

        if (evaluation is IEnumerable enumerable && !(evaluation is string))
        {
            foreach (var item in enumerable)
            {
                if (item is XElement el)
                {
                    results.Add(el.ToString());
                }
                else if (item is XAttribute attr)
                {
                    results.Add($"@{attr.Name.LocalName}=\"{attr.Value}\"");
                }
                else if (item is XText txt)
                {
                    if (!string.IsNullOrWhiteSpace(txt.Value))
                    {
                        results.Add(txt.Value.Trim());
                    }
                }
                else if (item is XComment comment)
                {
                    results.Add($"<!--{comment.Value}-->");
                }
                else if (item is XNode node)
                {
                    results.Add(node.ToString());
                }
                else if (item != null)
                {
                    results.Add(item.ToString() ?? string.Empty);
                }
            }
        }
        else if (evaluation is double d)
        {
            double rounded = Math.Round(d, 8);
            results.Add(rounded % 1 == 0 ? ((long)rounded).ToString() : rounded.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (evaluation is bool b)
        {
            results.Add(b ? "true" : "false");
        }
        else if (evaluation is string s)
        {
            if (!string.IsNullOrEmpty(s))
            {
                results.Add(s);
            }
        }
        else if (evaluation != null)
        {
            results.Add(evaluation.ToString() ?? string.Empty);
        }

        return results;
    }

    private static List<string> EvaluateXPathValuesInternal(XDocument doc, string xpathQuery)
    {
        var results = new List<string>();
        object evaluation = doc.XPathEvaluate(xpathQuery);

        if (evaluation is IEnumerable enumerable && !(evaluation is string))
        {
            foreach (var item in enumerable)
            {
                if (item is XElement el)
                {
                    if (!string.IsNullOrWhiteSpace(el.Value))
                        results.Add(el.Value.Trim());
                }
                else if (item is XAttribute attr)
                {
                    results.Add(attr.Value);
                }
                else if (item is XText txt)
                {
                    if (!string.IsNullOrWhiteSpace(txt.Value))
                        results.Add(txt.Value.Trim());
                }
                else if (item != null)
                {
                    string str = item.ToString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(str))
                        results.Add(str);
                }
            }
        }
        else if (evaluation is double d)
        {
            double rounded = Math.Round(d, 8);
            results.Add(rounded % 1 == 0 ? ((long)rounded).ToString() : rounded.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        else if (evaluation is bool b)
        {
            results.Add(b ? "true" : "false");
        }
        else if (evaluation is string s)
        {
            results.Add(s);
        }

        return results;
    }

    private static List<string> EvaluateXPathAttributesInternal(XDocument doc, string xpathQuery)
    {
        var results = new List<string>();
        object evaluation = doc.XPathEvaluate(xpathQuery);

        if (evaluation is IEnumerable enumerable && !(evaluation is string))
        {
            foreach (var item in enumerable)
            {
                if (item is XAttribute attrNode)
                {
                    string parent = attrNode.Parent != null ? $"{attrNode.Parent.Name.LocalName}: " : "";
                    results.Add($"{parent}@{attrNode.Name.LocalName}=\"{attrNode.Value}\"");
                }
                else if (item is XElement el)
                {
                    foreach (var elementAttr in el.Attributes())
                    {
                        if (!elementAttr.IsNamespaceDeclaration)
                        {
                            results.Add($"{el.Name.LocalName}: @{elementAttr.Name.LocalName}=\"{elementAttr.Value}\"");
                        }
                    }
                }
            }
        }

        return results;
    }

    private static XDocument RemoveNamespaces(XDocument doc)
    {
        var newDoc = new XDocument();
        if (doc.Root != null)
        {
            newDoc.Add(RemoveNamespaces(doc.Root));
        }
        return newDoc;
    }

    private static XElement RemoveNamespaces(XElement element)
    {
        var newElement = new XElement(element.Name.LocalName);
        foreach (var attr in element.Attributes())
        {
            if (!attr.IsNamespaceDeclaration)
            {
                newElement.Add(new XAttribute(attr.Name.LocalName, attr.Value));
            }
        }
        foreach (var node in element.Nodes())
        {
            if (node is XElement childEl)
            {
                newElement.Add(RemoveNamespaces(childEl));
            }
            else
            {
                newElement.Add(node);
            }
        }
        return newElement;
    }

    private static void FindMatchingQueryNodes(StructuredDataNode node, string query, List<StructuredDataNode> matches)
    {
        string normQuery = query.TrimStart('$', '.');
        string normPath = node.Path.TrimStart('$', '.');

        if (node.Path.Equals(query, StringComparison.OrdinalIgnoreCase) ||
            normPath.Equals(normQuery, StringComparison.OrdinalIgnoreCase) ||
            node.Name.Equals(normQuery, StringComparison.OrdinalIgnoreCase))
        {
            matches.Add(node);
        }

        foreach (var child in node.Children)
        {
            FindMatchingQueryNodes(child, query, matches);
        }
    }

    // ==========================================
    // 8. STRUCTURED TO TABULAR CONVERSIONS
    // ==========================================

    public static string ToCsv(string? text, char delimiter = ',')
    {
        var table = StructuredToTabular(text);
        return table != null ? TabularConverter.ToCsv(table, delimiter) : string.Empty;
    }

    public static string ToTsv(string? text)
    {
        var table = StructuredToTabular(text);
        return table != null ? TabularConverter.ToTsv(table) : string.Empty;
    }

    public static string ToMarkdownTable(string? text)
    {
        var table = StructuredToTabular(text);
        return table != null ? TabularConverter.ToMarkdownTable(table) : string.Empty;
    }

    public static TabularData? StructuredToTabular(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string trimmed = text.Trim();

        // 1. If it's already tabular, parse it directly
        var existingTable = TabularParser.DetectAndParse(text);
        if (existingTable != null && existingTable.Rows.Count > 0 && existingTable.Columns.Count > 1)
        {
            return existingTable;
        }

        // 2. Parse from JSON / YAML
        try
        {
            string json = (trimmed.StartsWith('{') || trimmed.StartsWith('['))
                ? trimmed
                : (TextBeautifier.IsYaml(trimmed) ? DeveloperTransformers.YamlToJson(trimmed, false) : string.Empty);

            if (!string.IsNullOrEmpty(json))
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Array)
                {
                    var columns = new List<string>();
                    var rows = new List<List<string>>();

                    foreach (var item in doc.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in item.EnumerateObject())
                            {
                                if (!columns.Contains(prop.Name))
                                    columns.Add(prop.Name);
                            }
                        }
                    }

                    if (columns.Count > 0)
                    {
                        foreach (var item in doc.RootElement.EnumerateArray())
                        {
                            var row = new List<string>();
                            if (item.ValueKind == JsonValueKind.Object)
                            {
                                foreach (var col in columns)
                                {
                                    if (item.TryGetProperty(col, out var val))
                                    {
                                        row.Add(val.ValueKind == JsonValueKind.String ? (val.GetString() ?? "") : val.GetRawText());
                                    }
                                    else
                                    {
                                        row.Add(string.Empty);
                                    }
                                }
                            }
                            rows.Add(row);
                        }

                        return new TabularData { Columns = columns, Rows = rows, HasHeaders = true };
                    }
                }
                else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    var columns = new List<string> { "Key", "Value" };
                    var rows = new List<List<string>>();
                    foreach (var prop in doc.RootElement.EnumerateObject())
                    {
                        string val = prop.Value.ValueKind == JsonValueKind.String ? (prop.Value.GetString() ?? "") : prop.Value.GetRawText();
                        rows.Add(new List<string> { prop.Name, val });
                    }
                    return new TabularData { Columns = columns, Rows = rows, HasHeaders = true };
                }
            }
        }
        catch
        {
            // Fallback
        }

        // 3. XML to Tabular
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                var xdoc = XDocument.Parse(trimmed);
                if (xdoc.Root != null)
                {
                    var childElements = xdoc.Root.Elements().ToList();
                    if (childElements.Count > 0 && childElements.All(e => e.HasElements || e.HasAttributes))
                    {
                        var columns = new List<string>();
                        foreach (var child in childElements)
                        {
                            foreach (var attr in child.Attributes())
                            {
                                string attrName = "@" + attr.Name.LocalName;
                                if (!columns.Contains(attrName)) columns.Add(attrName);
                            }
                            foreach (var sub in child.Elements())
                            {
                                string tagName = sub.Name.LocalName;
                                if (!columns.Contains(tagName)) columns.Add(tagName);
                            }
                        }

                        if (columns.Count > 0)
                        {
                            var rows = new List<List<string>>();
                            foreach (var child in childElements)
                            {
                                var row = new List<string>();
                                foreach (var col in columns)
                                {
                                    if (col.StartsWith('@'))
                                    {
                                        var attr = child.Attribute(col[1..]);
                                        row.Add(attr?.Value ?? "");
                                    }
                                    else
                                    {
                                        var sub = child.Element(col);
                                        row.Add(sub?.Value ?? "");
                                    }
                                }
                                rows.Add(row);
                            }

                            return new TabularData { Columns = columns, Rows = rows, HasHeaders = true };
                        }
                    }
                }
            }
            catch
            {
                // Fallback
            }
        }

        return null;
    }

    // ==========================================
    // 9. SCHEMA & CODE GENERATORS
    // ==========================================

    public static string ToTypeScriptInterfaces(string? text, string rootName = "Root")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string json = EnsureJson(text);
        if (string.IsNullOrEmpty(json)) return "// Invalid JSON/YAML input for TypeScript interface generation";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var interfaces = new Dictionary<string, List<(string Name, string Type)>>();
            InferTypeScriptTypes(doc.RootElement, rootName, interfaces);

            var sb = new StringBuilder();
            foreach (var (ifaceName, properties) in interfaces)
            {
                sb.AppendLine($"export interface {ifaceName} {{");
                foreach (var (propName, propType) in properties)
                {
                    sb.AppendLine($"  {propName}: {propType};");
                }
                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"// Error generating TypeScript interfaces: {ex.Message}";
        }
    }

    private static string InferTypeScriptTypes(JsonElement element, string currentName, Dictionary<string, List<(string Name, string Type)>> interfaces)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            string ifaceName = CaseTransformers.ChangeCase(currentName, TextCasing.PascalCase, false);
            if (string.IsNullOrEmpty(ifaceName)) ifaceName = "Item";

            if (!interfaces.ContainsKey(ifaceName))
            {
                var props = new List<(string Name, string Type)>();
                interfaces[ifaceName] = props;

                foreach (var prop in element.EnumerateObject())
                {
                    string propType = InferTypeScriptTypes(prop.Value, prop.Name, interfaces);
                    props.Add((prop.Name, propType));
                }
            }

            return ifaceName;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var first = element.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined)
            {
                return "any[]";
            }
            string itemType = InferTypeScriptTypes(first, currentName + "Item", interfaces);
            return $"{itemType}[]";
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => "number",
            JsonValueKind.True or JsonValueKind.False => "boolean",
            _ => "any"
        };
    }

    public static string ToCSharpClasses(string? text, string rootName = "Root")
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        string json = EnsureJson(text);
        if (string.IsNullOrEmpty(json)) return "// Invalid JSON/YAML input for C# class generation";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var classes = new Dictionary<string, List<(string JsonName, string PropName, string Type)>>();
            InferCSharpTypes(doc.RootElement, rootName, classes);

            var sb = new StringBuilder();
            foreach (var (clsName, properties) in classes)
            {
                sb.AppendLine($"public class {clsName}");
                sb.AppendLine("{");
                foreach (var (jsonName, propName, propType) in properties)
                {
                    if (!string.Equals(jsonName, propName, StringComparison.Ordinal))
                    {
                        sb.AppendLine($"    [JsonPropertyName(\"{jsonName}\")]");
                    }
                    string init = propType.StartsWith("List<") ? " = new();" : (propType == "string" ? " = string.Empty;" : "");
                    sb.AppendLine($"    public {propType} {propName} {{ get; set; }}{init}");
                }
                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString().Trim();
        }
        catch (Exception ex)
        {
            return $"// Error generating C# classes: {ex.Message}";
        }
    }

    private static string InferCSharpTypes(JsonElement element, string currentName, Dictionary<string, List<(string JsonName, string PropName, string Type)>> classes)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            string clsName = CaseTransformers.ChangeCase(currentName, TextCasing.PascalCase, false);
            if (string.IsNullOrEmpty(clsName)) clsName = "Item";

            if (!classes.ContainsKey(clsName))
            {
                var props = new List<(string JsonName, string PropName, string Type)>();
                classes[clsName] = props;

                foreach (var prop in element.EnumerateObject())
                {
                    string propName = CaseTransformers.ChangeCase(prop.Name, TextCasing.PascalCase, false);
                    string propType = InferCSharpTypes(prop.Value, prop.Name, classes);
                    props.Add((prop.Name, propName, propType));
                }
            }

            return clsName;
        }

        if (element.ValueKind == JsonValueKind.Array)
        {
            var first = element.EnumerateArray().FirstOrDefault();
            if (first.ValueKind == JsonValueKind.Undefined)
            {
                return "List<object>";
            }
            string itemType = InferCSharpTypes(first, currentName + "Item", classes);
            return $"List<{itemType}>";
        }

        return element.ValueKind switch
        {
            JsonValueKind.String => "string",
            JsonValueKind.Number => element.TryGetInt64(out _) ? "int" : "double",
            JsonValueKind.True or JsonValueKind.False => "bool",
            _ => "object"
        };
    }

    public static string ToJsonSchema(string? text, string title = "Schema")
    {
        if (string.IsNullOrWhiteSpace(text)) return "{}";
        string json = EnsureJson(text);
        if (string.IsNullOrEmpty(json)) return "{}";

        try
        {
            using var doc = JsonDocument.Parse(json);
            var rootSchema = GenerateJsonSchemaElement(doc.RootElement, title);
            return JsonSerializer.Serialize(rootSchema, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            return $"{{\"error\": \"Error generating JSON Schema: {ex.Message}\"}}";
        }
    }

    private static Dictionary<string, object?> GenerateJsonSchemaElement(JsonElement element, string? title = null)
    {
        var schema = new Dictionary<string, object?>();
        if (!string.IsNullOrEmpty(title))
        {
            schema["$schema"] = "http://json-schema.org/draft-07/schema#";
            schema["title"] = title;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                schema["type"] = "object";
                var properties = new Dictionary<string, object?>();
                var required = new List<string>();

                foreach (var prop in element.EnumerateObject())
                {
                    properties[prop.Name] = GenerateJsonSchemaElement(prop.Value);
                    required.Add(prop.Name);
                }

                schema["properties"] = properties;
                if (required.Count > 0)
                {
                    schema["required"] = required;
                }
                break;

            case JsonValueKind.Array:
                schema["type"] = "array";
                var first = element.EnumerateArray().FirstOrDefault();
                if (first.ValueKind != JsonValueKind.Undefined)
                {
                    schema["items"] = GenerateJsonSchemaElement(first);
                }
                break;

            case JsonValueKind.String:
                schema["type"] = "string";
                break;

            case JsonValueKind.Number:
                schema["type"] = element.TryGetInt64(out _) ? "integer" : "number";
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                schema["type"] = "boolean";
                break;

            case JsonValueKind.Null:
                schema["type"] = "null";
                break;

            default:
                schema["type"] = "any";
                break;
        }

        return schema;
    }

    private static string EnsureJson(string text)
    {
        string trimmed = text.Trim();
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            return trimmed;
        }
        if (TextBeautifier.IsYaml(trimmed))
        {
            return DeveloperTransformers.YamlToJson(trimmed, false);
        }
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            return XmlToJson(trimmed, false);
        }
        return string.Empty;
    }

    private static object? ConvertJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElement(prop.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElement(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt64(out long l)) return l;
                if (element.TryGetDouble(out double d)) return d;
                return element.GetRawText();

            case JsonValueKind.True: return true;
            case JsonValueKind.False: return false;
            case JsonValueKind.Null: return null;

            default:
                return element.GetRawText();
        }
    }

    private static string FormatScalarForDisplay(string value, StructuredNodeType nodeType)
    {
        if (nodeType == StructuredNodeType.String)
        {
            return $"\"{value}\"";
        }
        return value;
    }

    private static string SanitizeXmlName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "item";
        var sb = new StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i == 0 && (char.IsLetter(c) || c == '_'))
            {
                sb.Append(c);
            }
            else if (i > 0 && (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.'))
            {
                sb.Append(c);
            }
            else
            {
                sb.Append('_');
            }
        }
        return sb.Length > 0 ? sb.ToString() : "item";
    }

    private static object? ParseScalarValue(string str)
    {
        string trimmed = str.Trim();
        if (trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
        if (trimmed.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
        if (trimmed.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

        if (trimmed.StartsWith('"') && trimmed.EndsWith('"') && trimmed.Length >= 2)
            return trimmed[1..^1];

        if (long.TryParse(trimmed, out long l)) return l;
        if (double.TryParse(trimmed, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d)) return d;

        return str;
    }
}
