namespace TextForge.Core.Transformers;

public interface ICaseTransformer
{
    string ChangeCase(string? text, TextCasing casing, bool perLine = true);
}
