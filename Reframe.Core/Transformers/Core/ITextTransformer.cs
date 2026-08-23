namespace Reframe.Core.Transformers;

public interface ITextTransformer
{
    string Name { get; }
    string Transform(string? input);
}
