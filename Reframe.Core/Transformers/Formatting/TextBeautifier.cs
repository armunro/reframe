namespace Reframe.Core.Transformers.Formatting;

/// <summary>
/// Provides beautification/formatting capabilities for structured text formats such as JSON, XML, XHTML/HTML, YAML, etc.
/// </summary>
public static class TextBeautifier
{
    public static ITextBeautifier Instance { get; set; } = TextBeautifierService.Instance;

    public static bool CanBeautify(string? text) => Instance.CanBeautify(text);
    public static bool IsYaml(string? text) => Instance.IsYaml(text);
    public static string Beautify(string? text) => Instance.Beautify(text);
    public static string BeautifyJson(string? text, bool indented = true) => Instance.BeautifyJson(text, indented);
    public static string BeautifyXml(string? text) => Instance.BeautifyXml(text);
    public static string BeautifyYaml(string? text) => Instance.BeautifyYaml(text);
}
