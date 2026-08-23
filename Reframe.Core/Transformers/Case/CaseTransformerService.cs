using System.Text;
using System.Text.RegularExpressions;

namespace Reframe.Core.Transformers;

public class CaseTransformerService : ICaseTransformer
{
    private static readonly Regex WordSplitRegex = new(@"[_\-\s\.\/\\]+|(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])|(?<=[0-9])(?=[A-Za-z])|(?<=[A-Za-z])(?=[0-9])", RegexOptions.Compiled);

    public static CaseTransformerService Instance { get; } = new();

    public string ChangeCase(string? text, TextCasing casing, bool perLine = true)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;

        if (perLine)
        {
            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var results = lines.Select(l => ConvertWordCase(l, casing));
            return string.Join(Environment.NewLine, results);
        }

        return ConvertWordCase(text, casing);
    }

    private static string ConvertWordCase(string input, TextCasing casing)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        var words = WordSplitRegex.Split(input).Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        if (words.Count == 0) return input;

        return casing switch
        {
            TextCasing.CamelCase => ToCamelCase(words),
            TextCasing.PascalCase => ToPascalCase(words),
            TextCasing.SnakeCase => string.Join("_", words.Select(w => w.ToLowerInvariant())),
            TextCasing.KebabCase => string.Join("-", words.Select(w => w.ToLowerInvariant())),
            TextCasing.ConstantCase => string.Join("_", words.Select(w => w.ToUpperInvariant())),
            TextCasing.TitleCase => ToTitleCase(words),
            TextCasing.UpperCase => input.ToUpperInvariant(),
            TextCasing.LowerCase => input.ToLowerInvariant(),
            TextCasing.DotCase => string.Join(".", words.Select(w => w.ToLowerInvariant())),
            TextCasing.PathCase => string.Join("/", words.Select(w => w.ToLowerInvariant())),
            _ => input
        };
    }

    private static string ToCamelCase(List<string> words)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < words.Count; i++)
        {
            string word = words[i].ToLowerInvariant();
            if (i == 0)
            {
                sb.Append(word);
            }
            else
            {
                sb.Append(Capitalize(word));
            }
        }
        return sb.ToString();
    }

    private static string ToPascalCase(List<string> words)
    {
        var sb = new StringBuilder();
        foreach (var word in words)
        {
            sb.Append(Capitalize(word.ToLowerInvariant()));
        }
        return sb.ToString();
    }

    private static string ToTitleCase(List<string> words)
    {
        return string.Join(" ", words.Select(w => Capitalize(w.ToLowerInvariant())));
    }

    private static string Capitalize(string str)
    {
        if (string.IsNullOrEmpty(str)) return str;
        return char.ToUpperInvariant(str[0]) + str[1..];
    }
}
