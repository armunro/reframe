namespace Reframe.Core.Transformers.Case;

public enum TextCasing
{
    CamelCase,
    PascalCase,
    SnakeCase,
    KebabCase,
    ConstantCase,
    TitleCase,
    UpperCase,
    LowerCase,
    DotCase,
    PathCase
}

public static class CaseTransformers
{
    public static ICaseTransformer Instance { get; set; } = CaseTransformerService.Instance;

    public static string ChangeCase(string? text, TextCasing casing, bool perLine = true)
    {
        return Instance.ChangeCase(text, casing, perLine);
    }
}
