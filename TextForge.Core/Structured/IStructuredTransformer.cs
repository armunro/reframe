using TextForge.Core.Transformers;

namespace TextForge.Core.Structured;

public interface IStructuredTransformer
{
    string XmlToJson(string? xml, bool indented = true);
    string JsonToXml(string? json, string rootElementName = "root", bool indented = true);
    string XmlToYaml(string? xml);
    string YamlToXml(string? yaml, string rootElementName = "root", bool indented = true);
    string MinifyJson(string? json);
    string MinifyXml(string? xml);
    string Flatten(string? text, string separator = ".");
    string FlattenToFlatJson(string? text, string separator = ".");
    string Unflatten(string? flatText, string format = "JSON");
    string SortKeys(string? text, bool descending = false);
    string ExtractPaths(string? text);
    string ExtractKeys(string? text);
    string ExtractValues(string? text);
    string ConvertKeysCase(string? text, TextCasing casing);
    string PickKeys(string? text, string? keyList);
    string OmitKeys(string? text, string? keyList);
    string RemoveNullsAndEmpty(string? text);
    string QueryPath(string? text, string? query);
    string QueryXPath(string? xml, string? xpathQuery);
    string ExtractXPathValues(string? xml, string? xpathQuery);
    string ExtractXPathAttributes(string? xml, string? xpathQuery = "//@*");
    string ToCsv(string? text, char delimiter = ',');
    string ToTsv(string? text);
    string ToMarkdownTable(string? text);
    string ToTypeScriptInterfaces(string? text, string rootName = "Root");
    string ToCSharpClasses(string? text, string rootName = "Root");
    string ToJsonSchema(string? text, string title = "Schema");
}
