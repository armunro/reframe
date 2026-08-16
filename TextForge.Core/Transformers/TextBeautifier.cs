using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace TextForge.Core.Transformers;

/// <summary>
/// Provides beautification/formatting capabilities for structured text formats such as JSON, XML, XHTML/HTML, etc.
/// </summary>
public static class TextBeautifier
{
    /// <summary>
    /// Checks whether the text matches a known structured format that can be beautified (e.g. JSON or XML).
    /// </summary>
    public static bool CanBeautify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        string trimmed = text.Trim();

        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                using var doc = JsonDocument.Parse(trimmed);
                return doc.RootElement.ValueKind == JsonValueKind.Object || 
                       doc.RootElement.ValueKind == JsonValueKind.Array;
            }
            catch
            {
                return false;
            }
        }

        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            try
            {
                XDocument.Parse(trimmed);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to beautify the text if it is in a format that supports beautification (e.g., JSON, XML).
    /// If beautification is not possible or the text is not a valid structured format, returns the original text.
    /// </summary>
    public static string Beautify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return text ?? string.Empty;

        string trimmed = text.Trim();

        // 1. JSON (Object or Array)
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            string beautifiedJson = BeautifyJson(trimmed);
            if (!string.IsNullOrEmpty(beautifiedJson) && beautifiedJson != trimmed)
            {
                return beautifiedJson;
            }
        }

        // 2. XML / XHTML / HTML (well-formed markup)
        if (trimmed.StartsWith('<') && trimmed.EndsWith('>'))
        {
            string beautifiedXml = BeautifyXml(trimmed);
            if (!string.IsNullOrEmpty(beautifiedXml) && beautifiedXml != trimmed)
            {
                return beautifiedXml;
            }
        }

        return text;
    }

    /// <summary>
    /// Formats and indents JSON text.
    /// </summary>
    public static string BeautifyJson(string? text, bool indented = true)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            using var doc = JsonDocument.Parse(text.Trim());
            if (doc.RootElement.ValueKind == JsonValueKind.Object ||
                doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = indented,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };
                return JsonSerializer.Serialize(doc.RootElement, options);
            }
            return text;
        }
        catch
        {
            return text;
        }
    }

    /// <summary>
    /// Formats and indents XML / XHTML text.
    /// </summary>
    public static string BeautifyXml(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;

        try
        {
            var doc = XDocument.Parse(text.Trim());
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                OmitXmlDeclaration = doc.Declaration == null
            };
            var sb = new StringBuilder();
            using (var xw = XmlWriter.Create(sb, settings))
            {
                doc.Save(xw);
            }
            return sb.ToString().TrimEnd();
        }
        catch
        {
            return text;
        }
    }
}
