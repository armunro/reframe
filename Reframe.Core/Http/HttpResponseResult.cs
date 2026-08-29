namespace Reframe.Core.Http;

/// <summary>
/// Represents the result of an executed HTTP request.
/// </summary>
public class HttpResponseResult
{
    public bool IsSuccess { get; set; }
    public int StatusCode { get; set; }
    public string StatusDescription { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long ContentLength { get; set; }
    public TimeSpan Elapsed { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public string? ErrorMessage { get; set; }

    public string SummaryText
    {
        get
        {
            if (!IsSuccess && !string.IsNullOrEmpty(ErrorMessage))
            {
                return $"Error: {ErrorMessage} ({(int)Elapsed.TotalMilliseconds} ms)";
            }

            string sizeStr = ContentLength > 0 ? FormatBytes(ContentLength) : $"{Content.Length} chars";
            return $"{StatusCode} {StatusDescription} • {sizeStr} • {(int)Elapsed.TotalMilliseconds} ms";
        }
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.#} {sizes[order]}";
    }
}
