namespace Reframe.Core.Transformers.Case;

public interface ICaseTransformer
{
    string ChangeCase(string? text, TextCasing casing, bool perLine = true);
}
