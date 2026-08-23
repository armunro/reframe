using Reframe.Core.Structured.Models;

namespace Reframe.Core.Structured.Parsers;

public class StructuredDataParseResult
{
    public bool Success { get; init; }
    public string Format { get; init; } = "None";
    public string? ErrorMessage { get; init; }
    public List<StructuredDataNode> RootNodes { get; init; } = new();
    public int TotalNodeCount { get; init; }
}

public static class StructuredDataParser
{
    public static IStructuredDataParser Instance { get; set; } = StructuredDataParserService.Instance;

    public static StructuredDataParseResult Parse(string? text) => Instance.Parse(text);
    public static StructuredDataParseResult TryParseJson(string json) => Instance.TryParseJson(json);
    public static StructuredDataParseResult TryParseXml(string xml) => Instance.TryParseXml(xml);
    public static StructuredDataParseResult TryParseYaml(string yaml) => Instance.TryParseYaml(yaml);
}
