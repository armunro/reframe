using System.Collections;
using System.Text.Json;
using System.Xml.Linq;
using YamlDotNet.Serialization;

namespace Reframe.Core.Structured;

public class StructuredDataParserService : IStructuredDataParser
{
    public static StructuredDataParserService Instance { get; } = new();

    public StructuredDataParseResult Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new StructuredDataParseResult
            {
                Success = false,
                Format = "Empty",
                ErrorMessage = "Input text is empty"
            };
        }

        string trimmed = text.Trim();

        // 1. Try JSON
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) || (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            var jsonRes = TryParseJson(trimmed);
            if (jsonRes.Success) return jsonRes;
        }

        // 2. Try XML
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            var xmlRes = TryParseXml(trimmed);
            if (xmlRes.Success) return xmlRes;
        }

        // 3. Try YAML
        if (IsYamlCandidate(trimmed))
        {
            var yamlRes = TryParseYaml(trimmed);
            if (yamlRes.Success) return yamlRes;
        }

        // Fallback: Try JSON without brace guarantee
        var fallbackJson = TryParseJson(trimmed);
        if (fallbackJson.Success) return fallbackJson;

        // Fallback: Try XML
        var fallbackXml = TryParseXml(trimmed);
        if (fallbackXml.Success) return fallbackXml;

        return new StructuredDataParseResult
        {
            Success = false,
            Format = "Unknown",
            ErrorMessage = "Could not parse input as JSON, YAML, or XML"
        };
    }

    public StructuredDataParseResult TryParseJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var rootNodes = new List<StructuredDataNode>();
            int count = 0;

            if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                var root = CreateJsonNode("$", doc.RootElement, "$", ref count);
                rootNodes.Add(root);
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var root = CreateJsonNode("$", doc.RootElement, "$", ref count);
                rootNodes.Add(root);
            }
            else
            {
                var root = CreateJsonNode("$", doc.RootElement, "$", ref count);
                rootNodes.Add(root);
            }

            return new StructuredDataParseResult
            {
                Success = true,
                Format = "JSON",
                RootNodes = rootNodes,
                TotalNodeCount = count
            };
        }
        catch (Exception ex)
        {
            return new StructuredDataParseResult
            {
                Success = false,
                Format = "JSON",
                ErrorMessage = ex.Message
            };
        }
    }

    public StructuredDataParseResult TryParseXml(string xml)
    {
        try
        {
            var doc = XDocument.Parse(xml);
            if (doc.Root == null)
            {
                return new StructuredDataParseResult
                {
                    Success = false,
                    Format = "XML",
                    ErrorMessage = "No root element found in XML"
                };
            }

            var rootNodes = new List<StructuredDataNode>();
            int count = 0;
            var root = CreateXmlNode(doc.Root, "/" + doc.Root.Name.LocalName, ref count);
            rootNodes.Add(root);

            return new StructuredDataParseResult
            {
                Success = true,
                Format = "XML",
                RootNodes = rootNodes,
                TotalNodeCount = count
            };
        }
        catch (Exception ex)
        {
            return new StructuredDataParseResult
            {
                Success = false,
                Format = "XML",
                ErrorMessage = ex.Message
            };
        }
    }

    public StructuredDataParseResult TryParseYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize(new StringReader(yaml));

            if (result == null)
            {
                return new StructuredDataParseResult
                {
                    Success = false,
                    Format = "YAML",
                    ErrorMessage = "YAML document is empty"
                };
            }

            var rootNodes = new List<StructuredDataNode>();
            int count = 0;
            var root = CreateYamlNode("$", result, "$", ref count);
            rootNodes.Add(root);

            return new StructuredDataParseResult
            {
                Success = true,
                Format = "YAML",
                RootNodes = rootNodes,
                TotalNodeCount = count
            };
        }
        catch (Exception ex)
        {
            return new StructuredDataParseResult
            {
                Success = false,
                Format = "YAML",
                ErrorMessage = ex.Message
            };
        }
    }

    private static StructuredDataNode CreateJsonNode(string name, JsonElement element, string currentPath, ref int totalCount)
    {
        totalCount++;
        var node = new StructuredDataNode
        {
            Name = name,
            Path = currentPath
        };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                node.NodeType = StructuredNodeType.Object;
                foreach (var prop in element.EnumerateObject())
                {
                    string propPath = currentPath == "$" ? $"$.{prop.Name}" : $"{currentPath}.{prop.Name}";
                    node.Children.Add(CreateJsonNode(prop.Name, prop.Value, propPath, ref totalCount));
                }
                break;

            case JsonValueKind.Array:
                node.NodeType = StructuredNodeType.Array;
                int index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    string itemPath = $"{currentPath}[{index}]";
                    node.Children.Add(CreateJsonNode($"[{index}]", item, itemPath, ref totalCount));
                    index++;
                }
                break;

            case JsonValueKind.String:
                node.NodeType = StructuredNodeType.String;
                node.Value = element.GetString();
                break;

            case JsonValueKind.Number:
                node.NodeType = StructuredNodeType.Number;
                node.Value = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                node.NodeType = StructuredNodeType.Boolean;
                node.Value = element.GetBoolean() ? "true" : "false";
                break;

            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                node.NodeType = StructuredNodeType.Null;
                node.Value = null;
                break;

            default:
                node.NodeType = StructuredNodeType.String;
                node.Value = element.GetRawText();
                break;
        }

        return node;
    }

    private static StructuredDataNode CreateXmlNode(XElement element, string currentPath, ref int totalCount)
    {
        totalCount++;
        var node = new StructuredDataNode
        {
            Name = $"<{element.Name.LocalName}>",
            NodeType = StructuredNodeType.Element,
            Path = currentPath
        };

        // Attributes
        foreach (var attr in element.Attributes())
        {
            totalCount++;
            node.Children.Add(new StructuredDataNode
            {
                Name = $"@{attr.Name.LocalName}",
                Value = attr.Value,
                NodeType = StructuredNodeType.Attribute,
                Path = $"{currentPath}/@{attr.Name.LocalName}"
            });
        }

        // Child Elements
        var groupedChildren = element.Elements().GroupBy(e => e.Name.LocalName).ToList();
        foreach (var group in groupedChildren)
        {
            int idx = 1;
            bool multiple = group.Count() > 1;
            foreach (var child in group)
            {
                string childPath = multiple
                    ? $"{currentPath}/{child.Name.LocalName}[{idx}]"
                    : $"{currentPath}/{child.Name.LocalName}";
                node.Children.Add(CreateXmlNode(child, childPath, ref totalCount));
                idx++;
            }
        }

        // If no child elements, check for inner text
        if (!element.HasElements && !string.IsNullOrEmpty(element.Value))
        {
            node.Value = element.Value;
        }

        return node;
    }

    private static StructuredDataNode CreateYamlNode(string name, object? obj, string currentPath, ref int totalCount)
    {
        totalCount++;
        var node = new StructuredDataNode
        {
            Name = name,
            Path = currentPath
        };

        if (obj == null)
        {
            node.NodeType = StructuredNodeType.Null;
            node.Value = null;
            return node;
        }

        if (obj is IDictionary dict)
        {
            node.NodeType = StructuredNodeType.Object;
            foreach (DictionaryEntry entry in dict)
            {
                string key = entry.Key?.ToString() ?? string.Empty;
                string childPath = currentPath == "$" ? $"$.{key}" : $"{currentPath}.{key}";
                node.Children.Add(CreateYamlNode(key, entry.Value, childPath, ref totalCount));
            }
            return node;
        }

        if (obj is IList list)
        {
            node.NodeType = StructuredNodeType.Array;
            int idx = 0;
            foreach (var item in list)
            {
                string itemPath = $"{currentPath}[{idx}]";
                node.Children.Add(CreateYamlNode($"[{idx}]", item, itemPath, ref totalCount));
                idx++;
            }
            return node;
        }

        string strVal = obj.ToString() ?? string.Empty;
        if (obj is bool b)
        {
            node.NodeType = StructuredNodeType.Boolean;
            node.Value = b ? "true" : "false";
        }
        else if (long.TryParse(strVal, out _) || double.TryParse(strVal, System.Globalization.CultureInfo.InvariantCulture, out _))
        {
            node.NodeType = StructuredNodeType.Number;
            node.Value = strVal;
        }
        else
        {
            node.NodeType = StructuredNodeType.String;
            node.Value = strVal;
        }

        return node;
    }

    private static bool IsYamlCandidate(string text)
    {
        if (text.StartsWith('{') || text.StartsWith('<')) return false;
        return text.StartsWith("---") ||
               text.StartsWith("- ") ||
               (text.Contains(':') && (text.Contains('\n') || text.Contains('\r')));
    }
}
