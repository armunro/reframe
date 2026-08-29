using System.Text;

namespace Reframe.Core.Http;

/// <summary>
/// Represents a structured HTTP request definition parsed from cURL or user input.
/// </summary>
public class HttpRequestDefinition
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = string.Empty;
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? Body { get; set; }
    public int TimeoutSeconds { get; set; } = 30;
    public bool FollowRedirects { get; set; } = true;
    public bool IgnoreSslErrors { get; set; } = false;

    /// <summary>
    /// Converts this request definition into an equivalent cURL command string.
    /// </summary>
    public string ToCurlCommand()
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        var method = (Method ?? "GET").ToUpperInvariant();
        if (method != "GET" || !string.IsNullOrEmpty(Body))
        {
            sb.Append($" -X {method}");
        }

        if (!string.IsNullOrWhiteSpace(Url))
        {
            sb.Append($" \"{EscapeQuotes(Url)}\"");
        }

        foreach (var header in Headers)
        {
            sb.Append($" -H \"{EscapeQuotes(header.Key)}: {EscapeQuotes(header.Value)}\"");
        }

        if (!string.IsNullOrEmpty(Body))
        {
            sb.Append($" -d \"{EscapeQuotes(Body)}\"");
        }

        if (IgnoreSslErrors)
        {
            sb.Append(" -k");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates a C# HttpClient snippet for this request.
    /// </summary>
    public string ToCSharpSnippet()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// C# HttpClient Request");
        sb.AppendLine("using var client = new HttpClient();");
        
        var method = (Method ?? "GET").ToUpperInvariant();
        var url = string.IsNullOrWhiteSpace(Url) ? "https://example.com" : Url;

        sb.AppendLine($"using var request = new HttpRequestMessage(HttpMethod.{ToCSharpHttpMethod(method)}, \"{EscapeQuotes(url)}\");");

        foreach (var header in Headers)
        {
            if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            sb.AppendLine($"request.Headers.TryAddWithoutValidation(\"{EscapeQuotes(header.Key)}\", \"{EscapeQuotes(header.Value)}\");");
        }

        if (!string.IsNullOrEmpty(Body))
        {
            string contentType = Headers.TryGetValue("Content-Type", out var ct) ? ct : "application/json";
            sb.AppendLine($"request.Content = new StringContent(@\"{Body.Replace("\"", "\"\"")}\", System.Text.Encoding.UTF8, \"{contentType}\");");
        }

        sb.AppendLine("var response = await client.SendAsync(request);");
        sb.AppendLine("var content = await response.Content.ReadAsStringAsync();");

        return sb.ToString();
    }

    private static string ToCSharpHttpMethod(string method) => method switch
    {
        "GET" => "Get",
        "POST" => "Post",
        "PUT" => "Put",
        "DELETE" => "Delete",
        "PATCH" => "Patch",
        "HEAD" => "Head",
        "OPTIONS" => "Options",
        _ => $"new HttpMethod(\"{method}\")"
    };

    private static string EscapeQuotes(string text)
    {
        return text.Replace("\"", "\\\"");
    }
}
