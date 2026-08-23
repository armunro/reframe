namespace Reframe.Core.Transformers.Core;

public interface ITextTransformer
{
    string Name { get; }
    string Transform(string? input);
}
