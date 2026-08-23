using Reframe.Core.Transformers.Case;

namespace Reframe.Core.Structured.Transformers;

public class StructuredTransformerService : IStructuredTransformer
{
    public static StructuredTransformerService Instance { get; } = new();

    public string XmlToJson(string? xml, bool indented = true) => StructuredTransformers.XmlToJson(xml, indented);
    public string JsonToXml(string? json, string rootElementName = "root", bool indented = true) => StructuredTransformers.JsonToXml(json, rootElementName, indented);
    public string XmlToYaml(string? xml) => StructuredTransformers.XmlToYaml(xml);
    public string YamlToXml(string? yaml, string rootElementName = "root", bool indented = true) => StructuredTransformers.YamlToXml(yaml, rootElementName, indented);
    public string MinifyJson(string? json) => StructuredTransformers.MinifyJson(json);
    public string MinifyXml(string? xml) => StructuredTransformers.MinifyXml(xml);
    public string Flatten(string? text, string separator = ".") => StructuredTransformers.Flatten(text, separator);
    public string FlattenToFlatJson(string? text, string separator = ".") => StructuredTransformers.FlattenToFlatJson(text, separator);
    public string Unflatten(string? flatText, string format = "JSON") => StructuredTransformers.Unflatten(flatText, format);
    public string SortKeys(string? text, bool descending = false) => StructuredTransformers.SortKeys(text, descending);
    public string ExtractPaths(string? text) => StructuredTransformers.ExtractPaths(text);
    public string ExtractKeys(string? text) => StructuredTransformers.ExtractKeys(text);
    public string ExtractValues(string? text) => StructuredTransformers.ExtractValues(text);
    public string ConvertKeysCase(string? text, TextCasing casing) => StructuredTransformers.ConvertKeysCase(text, casing);
    public string PickKeys(string? text, string? keyList) => StructuredTransformers.PickKeys(text, keyList);
    public string OmitKeys(string? text, string? keyList) => StructuredTransformers.OmitKeys(text, keyList);
    public string RemoveNullsAndEmpty(string? text) => StructuredTransformers.RemoveNullsAndEmpty(text);
    public string QueryPath(string? text, string? query) => StructuredTransformers.QueryPath(text, query);
    public string QueryXPath(string? xml, string? xpathQuery) => StructuredTransformers.QueryXPath(xml, xpathQuery);
    public string ExtractXPathValues(string? xml, string? xpathQuery) => StructuredTransformers.ExtractXPathValues(xml, xpathQuery);
    public string ExtractXPathAttributes(string? xml, string? xpathQuery = "//@*") => StructuredTransformers.ExtractXPathAttributes(xml, xpathQuery);
    public string ToCsv(string? text, char delimiter = ',') => StructuredTransformers.ToCsv(text, delimiter);
    public string ToTsv(string? text) => StructuredTransformers.ToTsv(text);
    public string ToMarkdownTable(string? text) => StructuredTransformers.ToMarkdownTable(text);
    public string ToTypeScriptInterfaces(string? text, string rootName = "Root") => StructuredTransformers.ToTypeScriptInterfaces(text, rootName);
    public string ToCSharpClasses(string? text, string rootName = "Root") => StructuredTransformers.ToCSharpClasses(text, rootName);
    public string ToJsonSchema(string? text, string title = "Schema") => StructuredTransformers.ToJsonSchema(text, title);
}
