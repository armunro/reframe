using Reframe.Core.Transformers.Case;

namespace Reframe.Core.Transformers.Developer;

public static class DeveloperTransformers
{
    public static IDeveloperTransformer Instance { get; set; } = DeveloperTransformerService.Instance;

    public static string ToSqlInClause(string? text, bool multiLine = false, bool forceQuotes = false) => Instance.ToSqlInClause(text, multiLine, forceQuotes);
    public static string ToCSharpArray(string? text, string variableName = "items", bool asList = false) => Instance.ToCSharpArray(text, variableName, asList);
    public static string ToTypeScriptArray(string? text, string variableName = "items") => Instance.ToTypeScriptArray(text, variableName);
    public static string ToPythonList(string? text, string variableName = "items") => Instance.ToPythonList(text, variableName);
    public static string ToJsonArray(string? text, bool indented = true) => Instance.ToJsonArray(text, indented);
    public static string ToYamlArray(string? text) => Instance.ToYamlArray(text);
    public static string QueryStringToKeyValuePairs(string? text) => Instance.QueryStringToKeyValuePairs(text);
    public static string KeyValuePairsToQueryString(string? text) => Instance.KeyValuePairsToQueryString(text);
    public static string KeyValuePairsToJson(string? text) => Instance.KeyValuePairsToJson(text);
    public static string KeyValuePairsToYaml(string? text) => Instance.KeyValuePairsToYaml(text);
    public static string JsonToYaml(string? text) => Instance.JsonToYaml(text);
    public static string YamlToJson(string? text, bool indented = true) => Instance.YamlToJson(text, indented);
    public static string XmlToJson(string? text, bool indented = true) => Instance.XmlToJson(text, indented);
    public static string JsonToXml(string? text, string rootElementName = "root", bool indented = true) => Instance.JsonToXml(text, rootElementName, indented);
    public static string XmlToYaml(string? text) => Instance.XmlToYaml(text);
    public static string YamlToXml(string? text, string rootElementName = "root", bool indented = true) => Instance.YamlToXml(text, rootElementName, indented);
    public static string MinifyJson(string? text) => Instance.MinifyJson(text);
    public static string MinifyXml(string? text) => Instance.MinifyXml(text);
    public static string FlattenStructured(string? text, string separator = ".") => Instance.FlattenStructured(text, separator);
    public static string FlattenToFlatJson(string? text, string separator = ".") => Instance.FlattenToFlatJson(text, separator);
    public static string UnflattenStructured(string? text, string format = "JSON") => Instance.UnflattenStructured(text, format);
    public static string SortStructuredKeys(string? text, bool descending = false) => Instance.SortStructuredKeys(text, descending);
    public static string ExtractStructuredPaths(string? text) => Instance.ExtractStructuredPaths(text);
    public static string ExtractStructuredKeys(string? text) => Instance.ExtractStructuredKeys(text);
    public static string ExtractStructuredValues(string? text) => Instance.ExtractStructuredValues(text);
    public static string ConvertStructuredKeysCase(string? text, TextCasing casing) => Instance.ConvertStructuredKeysCase(text, casing);
    public static string PickStructuredKeys(string? text, string? keyList) => Instance.PickStructuredKeys(text, keyList);
    public static string OmitStructuredKeys(string? text, string? keyList) => Instance.OmitStructuredKeys(text, keyList);
    public static string RemoveNullsAndEmpty(string? text) => Instance.RemoveNullsAndEmpty(text);
    public static string QueryStructuredPath(string? text, string? query) => Instance.QueryStructuredPath(text, query);
    public static string QueryXPath(string? text, string? query) => Instance.QueryXPath(text, query);
    public static string ExtractXPathValues(string? text, string? query) => Instance.ExtractXPathValues(text, query);
    public static string ExtractXPathAttributes(string? text, string? query = "//@*") => Instance.ExtractXPathAttributes(text, query);
    public static string StructuredToCsv(string? text, char delimiter = ',') => Instance.StructuredToCsv(text, delimiter);
    public static string StructuredToTsv(string? text) => Instance.StructuredToTsv(text);
    public static string StructuredToMarkdown(string? text) => Instance.StructuredToMarkdown(text);
    public static string ToTypeScriptInterfaces(string? text, string rootName = "Root") => Instance.ToTypeScriptInterfaces(text, rootName);
    public static string ToCSharpClasses(string? text, string rootName = "Root") => Instance.ToCSharpClasses(text, rootName);
    public static string ToJsonSchema(string? text, string title = "Schema") => Instance.ToJsonSchema(text, title);
}
