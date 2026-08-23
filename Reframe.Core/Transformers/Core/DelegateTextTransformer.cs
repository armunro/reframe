namespace Reframe.Core.Transformers;

public class DelegateTextTransformer : ITextTransformer
{
    private readonly Func<string?, string> _transformFunc;

    public string Name { get; }

    public DelegateTextTransformer(string name, Func<string?, string> transformFunc)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        _transformFunc = transformFunc ?? throw new ArgumentNullException(nameof(transformFunc));
    }

    public string Transform(string? input) => _transformFunc(input);
}
