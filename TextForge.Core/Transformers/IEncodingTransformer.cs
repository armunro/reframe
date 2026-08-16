namespace TextForge.Core.Transformers;

public interface IEncodingTransformer
{
    string UrlEncode(string? text);
    string UrlDecode(string? text);
    string HtmlEncode(string? text);
    string HtmlDecode(string? text);
    string Base64Encode(string? text);
    string Base64Decode(string? text);
    string JwtDecode(string? token);
    string EscapeCSharpString(string? text);
    string UnescapeCSharpString(string? text);
    string FormatJsonString(string? text, bool indented = true);
    string FormatXmlString(string? text);
    string FormatYamlString(string? text);
    string JsonToYaml(string? text);
    string YamlToJson(string? text, bool indented = true);
    string XmlToJson(string? text, bool indented = true);
    string JsonToXml(string? text, string rootElementName = "root", bool indented = true);
    string XmlToYaml(string? text);
    string YamlToXml(string? text, string rootElementName = "root", bool indented = true);
    string MinifyJson(string? text);
    string MinifyXml(string? text);
    string FlattenStructured(string? text, string separator = ".");
    string FlattenToFlatJson(string? text, string separator = ".");
    string UnflattenStructured(string? text, string format = "JSON");
    string SortStructuredKeys(string? text, bool descending = false);
    string ExtractStructuredPaths(string? text);
    string ExtractStructuredKeys(string? text);
    string ExtractStructuredValues(string? text);
    string ConvertStructuredKeysCase(string? text, TextCasing casing);
    string PickStructuredKeys(string? text, string? keyList);
    string OmitStructuredKeys(string? text, string? keyList);
    string RemoveNullsAndEmpty(string? text);
    string QueryStructuredPath(string? text, string? query);
    string QueryXPath(string? text, string? query);
    string ExtractXPathValues(string? text, string? query);
    string ExtractXPathAttributes(string? text, string? query = "//@*");
    string StructuredToCsv(string? text, char delimiter = ',');
    string StructuredToTsv(string? text);
    string StructuredToMarkdown(string? text);
    string ToTypeScriptInterfaces(string? text, string rootName = "Root");
    string ToCSharpClasses(string? text, string rootName = "Root");
    string ToJsonSchema(string? text, string title = "Schema");
}
