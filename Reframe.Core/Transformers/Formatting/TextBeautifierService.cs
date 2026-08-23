using System.Collections;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Reframe.Core.Transformers.Formatting;

public class TextBeautifierService : ITextBeautifier
{
    public static TextBeautifierService Instance { get; } = new();

    public bool CanBeautify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return doc.RootElement.ValueKind == JsonValueKind.Object || 
                       doc.RootElement.ValueKind == JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }

        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                XDocument.Parse(trimmed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (IsYaml(trimmed))
        {
            return true;
        }

        return false;
    }

    public bool IsYaml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('<')) return false;

        bool hasYamlIndicator = trimmed.StartsWith("---") ||
                                trimmed.StartsWith("- ") ||
                                (trimmed.Contains(':') && (trimmed.Contains("\n") || trimmed.Contains("\r")));

        if (!hasYamlIndicator) return false;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize(new StringReader(trimmed));
            return result is IDictionary || result is IList;
        }
        catch
        {
            return false;
        }
    }

    public string Beautify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;

        string trimmed = text.Trim();

        // 1. JSON (Object or Array)
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            string beautifiedJson = BeautifyJson(trimmed);
            if (!string.IsNullOrEmpty(beautifiedJson) && beautifiedJson != trimmed)
            {
                return beautifiedJson;
            }
        }

        // 2. XML / XHTML / HTML (well-formed markup)
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            string beautifiedXml = BeautifyXml(trimmed);
            if (!string.IsNullOrEmpty(beautifiedXml) && beautifiedXml != trimmed)
            {
                return beautifiedXml;
            }
        }

        // 3. YAML
        if (IsYaml(trimmed))
        {
            string beautifiedYaml = BeautifyYaml(trimmed);
            if (!string.IsNullOrEmpty(beautifiedYaml) && beautifiedYaml != trimmed)
            {
                return beautifiedYaml;
            }
        }

        return text;
    }

    public string BeautifyJson(string? text, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(text.Trim());
            if (doc.RootElement.ValueKind == JsonValueKind.Object ||
                doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(doc.RootElement, options);
            }
            return text;
        }
        catch
        {
            return text;
        }
    }

    public string BeautifyXml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            var doc = XDocument.Parse(text.Trim());
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                OmitXmlDeclaration = doc.Declaration == null
            };
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, settings))
            {
                doc.Save(xw);
            }
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return text;
        }
    }

    public string BeautifyYaml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var yamlObject = deserializer.Deserialize(new StringReader(text.Trim()));
            if (yamlObject == null) return text;

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
}
