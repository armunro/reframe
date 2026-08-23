namespace Reframe.Core.Transformers;

public interface ITextBeautifier
{
    bool CanBeautify(string? text);
    bool IsYaml(string? text);
    string Beautify(string? text);
    string BeautifyJson(string? text, bool indented = true);
    string BeautifyXml(string? text);
    string BeautifyYaml(string? text);
}
