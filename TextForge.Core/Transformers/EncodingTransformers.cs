using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace TextForge.Core.Transformers;

public static class EncodingTransformers
{
    public static string UrlEncode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.UrlEncode(text);
    public static string UrlDecode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.UrlDecode(text);

    public static string HtmlEncode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlEncode(text);
    public static string HtmlDecode(string? text) => string.IsNullOrEmpty(text) ? string.Empty : WebUtility.HtmlDecode(text);

    public static string Base64Encode(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        var bytes = Encoding.UTF8.GetBytes(text);
        return Convert.ToBase64String(bytes);
    }

    public static string Base64Decode(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        try
        {
            string clean = text.Trim().Replace(" ", "+");
            // Add padding if missing
            int mod = clean.Length % 4;
            if (mod > 0) clean += new string('=', 4 - mod);

            var bytes = Convert.FromBase64String(clean);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (Exception ex)
        {
            return $"Error decoding Base64: {ex.Message}";
        }
    }

    public static string JwtDecode(string? token)
    {
        if (string.IsNullOrWhiteSpace(token)) return string.Empty;

        var parts = token.Trim().Split('.');
        if (parts.Length < 2) return "Invalid JWT token: Must contain at least 2 dot-separated segments.";

        var sb = new StringBuilder();

        try
        {
            string headerJson = DecodeBase64Url(parts[0]);
            sb.AppendLine("// Header");
            sb.AppendLine(FormatJsonString(headerJson));
            sb.AppendLine();

            string payloadJson = DecodeBase64Url(parts[1]);
            sb.AppendLine("// Payload");
            sb.AppendLine(FormatJsonString(payloadJson));

            if (parts.Length >= 3)
            {
                sb.AppendLine();
                sb.AppendLine($"// Signature: {parts[2]}");
            }

            return sb.ToString().TrimEnd();
        }
        catch (Exception ex)
        {
            return $"Error decoding JWT: {ex.Message}";
        }
    }

    public static string EscapeCSharpString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "\"\"";
        string escaped = text
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", "\\r")
            .Replace("\n", "\\n")
            .Replace("\t", "\\t");
        return $"\"{escaped}\"";
    }

    public static string UnescapeCSharpString(string? text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        string cleaned = text.Trim();
        if (cleaned.StartsWith('"') && cleaned.EndsWith('"') && cleaned.Length >= 2)
        {
            cleaned = cleaned[1..^1];
        }

        return Regex.Unescape(cleaned);
    }

    public static string FormatJsonString(string? text, bool indented = true)
    {
        return TextBeautifier.BeautifyJson(text, indented);
    }

    public static string FormatXmlString(string? text)
    {
        return TextBeautifier.BeautifyXml(text);
    }

    private static string DecodeBase64Url(string input)
    {
        string output = input.Replace('-', '+').Replace('_', '/');
        switch (output.Length % 4)
        {
            case 0: break;
            case 2: output += "=="; break;
            case 3: output += "="; break;
            default: throw new FormatException("Illegal base64url string!");
        }
        var converted = Convert.FromBase64String(output);
        return Encoding.UTF8.GetString(converted);
    }
}
