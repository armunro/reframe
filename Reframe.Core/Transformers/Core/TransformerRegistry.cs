namespace Reframe.Core.Transformers;

public interface ITransformerRegistry
{
    void Register(ITextTransformer transformer);
    void Register(string name, Func<string?, string> transformFunc);
    bool Unregister(string name);
    ITextTransformer? GetTransformer(string name);
    IEnumerable<ITextTransformer> GetAll();
}

public class TransformerRegistry : ITransformerRegistry
{
    private readonly Dictionary<string, ITextTransformer> _transformers = new(StringComparer.OrdinalIgnoreCase);

    public static TransformerRegistry Instance { get; } = new();

    public void Register(ITextTransformer transformer)
    {
        if (transformer == null) throw new ArgumentNullException(nameof(transformer));
        _transformers[transformer.Name] = transformer;
    }

    public void Register(string name, Func<string?, string> transformFunc)
    {
        Register(new DelegateTextTransformer(name, transformFunc));
    }

    public bool Unregister(string name)
    {
        if (string.IsNullOrEmpty(name)) return false;
        return _transformers.Remove(name);
    }

    public ITextTransformer? GetTransformer(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _transformers.TryGetValue(name, out var transformer) ? transformer : null;
    }

    public IEnumerable<ITextTransformer> GetAll() => _transformers.Values;
}
