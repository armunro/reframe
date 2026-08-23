namespace Reframe.Core.Transformers.Core;

public class TransformerPipeline : ITextTransformer
{
    private readonly List<ITextTransformer> _transformers;

    public string Name { get; set; }

    public IReadOnlyList<ITextTransformer> Transformers => _transformers.AsReadOnly();

    public TransformerPipeline(string name = "Pipeline")
    {
        Name = name;
        _transformers = new List<ITextTransformer>();
    }

    public TransformerPipeline(string name, IEnumerable<ITextTransformer> transformers)
    {
        Name = name;
        _transformers = transformers.ToList();
    }

    public TransformerPipeline Add(ITextTransformer transformer)
    {
        if (transformer == null) throw new ArgumentNullException(nameof(transformer));
        _transformers.Add(transformer);
        return this;
    }

    public TransformerPipeline Add(string name, Func<string?, string> transformFunc)
    {
        _transformers.Add(new DelegateTextTransformer(name, transformFunc));
        return this;
    }

    public string Transform(string? input)
    {
        string current = input ?? string.Empty;
        foreach (var transformer in _transformers)
        {
            current = transformer.Transform(current);
        }
        return current;
    }
}
