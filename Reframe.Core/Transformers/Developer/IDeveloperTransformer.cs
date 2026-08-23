using Reframe.Core.Transformers.Case;

namespace Reframe.Core.Transformers.Developer;

public interface IDeveloperTransformer
{
    string ToSqlInClause(string? text, bool multiLine = false, bool forceQuotes = false);
    string ToCSharpArray(string? text, string variableName = "items", bool asList = false);
    string ToTypeScriptArray(string? text, string variableName = "items");
    string ToPythonList(string? text, string variableName = "items");
    string ToJsonArray(string? text, bool indented = true);
    string ToYamlArray(string? text);
    string QueryStringToKeyValuePairs(string? text);
    string KeyValuePairsToQueryString(string? text);
    string KeyValuePairsToJson(string? text);
    string KeyValuePairsToYaml(string? text);
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
