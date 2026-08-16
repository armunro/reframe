namespace TextForge.Core.Structured;

public interface IStructuredDataParser
{
    StructuredDataParseResult Parse(string? text);
    StructuredDataParseResult TryParseJson(string json);
    StructuredDataParseResult TryParseXml(string xml);
    StructuredDataParseResult TryParseYaml(string yaml);
}
