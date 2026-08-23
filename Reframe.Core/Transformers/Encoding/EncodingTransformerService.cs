using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Reframe.Core.Structured.Transformers;
using Reframe.Core.Transformers.Case;
using Reframe.Core.Transformers.Developer;
using Reframe.Core.Transformers.Formatting;

namespace Reframe.Core.Transformers.Encoding;

public class EncodingTransformerService : IEncodingTransformer
{
    public static EncodingTransformerService Instance { get; } = new();

    public string UrlEncode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.UrlEncode(text);
    public string UrlDecode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.UrlDecode(text);

    public string HtmlEncode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlEncode(text);
    public string HtmlDecode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlDecode(text);

    public string Base64Encode(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    public string Base64Decode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        try
        {
            string clean = text.Trim().Replace(" ", "+");
            int mod = clean.Length % 4;
            if (mod > 0) clean += new string('=', 4 - mod);

            var bytes = Convert.FromBase64String(clean);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            return $"Error decoding Base64: {ex.Message}";
        }
    }

    public string JwtDecode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;

        var parts = token.Trim().Split('.');
        if (parts.Length < 2) return "Invalid JWT token: Must contain at least 2 dot-separated segments.";

        var sb = new StringBuilder();

        try
        {
            string headerJson = DecodeBase64Url(parts[0]);
            sb.AppendLine("// Header");
            sb.AppendLine(FormatJsonString(headerJson));
            sb.AppendLine();

            string payloadJson = DecodeBase64Url(parts[1]);
            sb.AppendLine("// Payload");
            sb.AppendLine(FormatJsonString(payloadJson));

            if (parts.Length >= 3)
            {
                sb.AppendLine();
                sb.AppendLine($"// Signature: {parts[2]}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error decoding JWT: {ex.Message}";
        }
    }

    public string EscapeCSharpString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "\"\"";
        string escaped = text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
        return $"\"{escaped}\"";
    }

    public string UnescapeCSharpString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string cleaned = text.Trim();
        if (cleaned.StartsWith('"') && cleaned.EndsWith('"') && cleaned.Length >= 2)
        {
            cleaned = cleaned[1..^1];
        }

        return Regex.Unescape(cleaned);
    }

    public string FormatJsonString(string? text, bool indented = true) => TextBeautifier.BeautifyJson(text, indented);
    public string FormatXmlString(string? text) => TextBeautifier.BeautifyXml(text);
    public string FormatYamlString(string? text) => TextBeautifier.BeautifyYaml(text);

    public string JsonToYaml(string? text) => DeveloperTransformers.JsonToYaml(text);
    public string YamlToJson(string? text, bool indented = true) => DeveloperTransformers.YamlToJson(text, indented);

    public string XmlToJson(string? text, bool indented = true) => StructuredTransformers.XmlToJson(text, indented);
    public string JsonToXml(string? text, string rootElementName = "root", bool indented = true) => StructuredTransformers.JsonToXml(text, rootElementName, indented);
    public string XmlToYaml(string? text) => StructuredTransformers.XmlToYaml(text);
    public string YamlToXml(string? text, string rootElementName = "root", bool indented = true) => StructuredTransformers.YamlToXml(text, rootElementName, indented);
    public string MinifyJson(string? text) => StructuredTransformers.MinifyJson(text);
    public string MinifyXml(string? text) => StructuredTransformers.MinifyXml(text);
    public string FlattenStructured(string? text, string separator = ".") => StructuredTransformers.Flatten(text, separator);
    public string FlattenToFlatJson(string? text, string separator = ".") => StructuredTransformers.FlattenToFlatJson(text, separator);
    public string UnflattenStructured(string? text, string format = "JSON") => StructuredTransformers.Unflatten(text, format);
    public string SortStructuredKeys(string? text, bool descending = false) => StructuredTransformers.SortKeys(text, descending);
    public string ExtractStructuredPaths(string? text) => StructuredTransformers.ExtractPaths(text);
    public string ExtractStructuredKeys(string? text) => StructuredTransformers.ExtractKeys(text);
    public string ExtractStructuredValues(string? text) => StructuredTransformers.ExtractValues(text);
    public string ConvertStructuredKeysCase(string? text, TextCasing casing) => StructuredTransformers.ConvertKeysCase(text, casing);
    public string PickStructuredKeys(string? text, string? keyList) => StructuredTransformers.PickKeys(text, keyList);
    public string OmitStructuredKeys(string? text, string? keyList) => StructuredTransformers.OmitKeys(text, keyList);
    public string RemoveNullsAndEmpty(string? text) => StructuredTransformers.RemoveNullsAndEmpty(text);
    public string QueryStructuredPath(string? text, string? query) => StructuredTransformers.QueryPath(text, query);
    public string QueryXPath(string? text, string? query) => StructuredTransformers.QueryXPath(text, query);
    public string ExtractXPathValues(string? text, string? query) => StructuredTransformers.ExtractXPathValues(text, query);
    public string ExtractXPathAttributes(string? text, string? query = "//@*") => StructuredTransformers.ExtractXPathAttributes(text, query);
    public string StructuredToCsv(string? text, char delimiter = ',') => StructuredTransformers.ToCsv(text, delimiter);
    public string StructuredToTsv(string? text) => StructuredTransformers.ToTsv(text);
    public string StructuredToMarkdown(string? text) => StructuredTransformers.ToMarkdownTable(text);
    public string ToTypeScriptInterfaces(string? text, string rootName = "Root") => StructuredTransformers.ToTypeScriptInterfaces(text, rootName);
    public string ToCSharpClasses(string? text, string rootName = "Root") => StructuredTransformers.ToCSharpClasses(text, rootName);
    public string ToJsonSchema(string? text, string title = "Schema") => StructuredTransformers.ToJsonSchema(text, title);

    private static string DecodeBase64Url(string input)
    {
        string output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new FormatException("Illegal base64url string!");
        }
        var converted = Convert.FromBase64String(output);
        return System.Text.Encoding.UTF8.GetString(converted);
    }
}
