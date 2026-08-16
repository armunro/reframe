using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;

namespace TextForge.Core.Tabular;

public static class TabularParser
{
    private static readonly char[] CandidateDelimiters = { '\t', ',', ';', '|' };

    public static ITabularParser Instance { get; set; } = TabularParserService.Instance;

    private static readonly HashSet<string> CommonHeaderKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "id", "identifier", "key", "pk", "fk", "code", "sku", "pin", "tenant", "guid", "uuid", "hash",
        "name", "first name", "firstname", "fname", "last name", "lastname", "lname", "full name", "fullname", "middle name", "server name", "servername",
        "title", "desc", "description", "summary", "details", "comment", "comments", "notes", "memo", "text", "body", "content", "message",
        "email", "e-mail", "mail", "phone", "mobile", "tel", "telephone", "fax", "contact",
        "address", "street", "city", "state", "province", "zip", "zipcode", "postal", "postalcode", "country", "region", "location", "lat", "latitude", "long", "longitude", "lng",
        "status", "state", "type", "category", "tag", "tags", "kind", "class", "genre", "priority", "severity",
        "date", "time", "datetime", "timestamp", "dob", "birthdate", "created", "created_at", "createdat", "updated", "updated_at", "updatedat", "modified", "deleted", "start", "end", "start_date", "end_date",
        "user", "username", "user_id", "userid", "author", "creator", "owner", "client", "customer", "company", "org", "organization", "account", "member", "employee", "vendor", "supplier",
        "role", "group", "team", "department", "dept", "division", "permission", "permissions", "access",
        "price", "cost", "amount", "total", "subtotal", "tax", "fee", "rate", "salary", "wage", "balance", "revenue", "income", "expense", "discount", "unit_price", "unitprice",
        "qty", "quantity", "count", "num", "number", "size", "length", "width", "height", "weight", "depth", "order", "seq", "sequence", "index", "rank", "position", "pos", "step", "level", "version", "ver",
        "url", "uri", "link", "href", "endpoint", "path", "ip", "ip address", "ipaddress", "host", "hostname", "server", "port", "domain", "protocol",
        "environment", "env", "pipeline", "service", "app", "application", "build", "agent", "branch", "commit", "release", "platform", "cluster", "node", "instance", "job", "task", "project", "repo", "repository", "feature", "module", "component", "config", "setting", "settings", "option", "options",
        "action", "event", "source", "target", "value", "val", "data", "result", "flag", "enabled", "active", "is_active", "isactive", "item", "product", "fruit", "color", "colour"
    };

    public static TabularData? DetectAndParse(string? text, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        return Instance.Parse(text, assumeHeader, surrogateHeaders);
    }

    public static TabularData Parse(string? text, char delimiter, bool? assumeHeader = null, IEnumerable<string>? surrogateHeaders = null)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            var emptyResult = new TabularData { Delimiter = delimiter, HasHeaders = assumeHeader ?? true };
            if (surrogateHeaders != null) emptyResult.OverrideHeaders(surrogateHeaders);
            return emptyResult;
        }

        var allRows = ParseCsvRecords(text, delimiter);
        if (allRows.Count == 0)
        {
            var emptyResult = new TabularData { Delimiter = delimiter, HasHeaders = assumeHeader ?? true };
            if (surrogateHeaders != null) emptyResult.OverrideHeaders(surrogateHeaders);
            return emptyResult;
        }

        bool hasHeaders = assumeHeader ?? DetectHasHeaders(allRows);
        var result = new TabularData { Delimiter = delimiter, HasHeaders = hasHeaders };

        if (hasHeaders && allRows.Count > 0)
        {
            result.Columns = allRows[0];
            // Ensure header names are not blank
            for (int i = 0; i < result.Columns.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(result.Columns[i]))
                {
                    result.Columns[i] = $"Column {i + 1}";
                }
            }
            result.Rows = allRows.Skip(1).ToList();
        }
        else
        {
            int maxCols = allRows.Max(r => r.Count);
            result.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
            result.Rows = allRows;
        }

        // Standardize row lengths to match column count
        int colCount = result.Columns.Count;
        foreach (var row in result.Rows)
        {
            while (row.Count < colCount) row.Add(string.Empty);
        }

        if (surrogateHeaders != null)
        {
            var list = surrogateHeaders.ToList();
            if (list.Count > 0)
            {
                result.OverrideHeaders(list);
            }
        }

        return result;
    }

    /// <summary>
    /// Parses a string of headers (comma, tab, pipe, semicolon, or newline separated) into a list of header names.
    /// </summary>
    public static List<string> ParseHeaderList(string? headerText, char? defaultDelimiter = null)
    {
        if (string.IsNullOrWhiteSpace(headerText)) return new List<string>();

        // Check if multi-line
        var lines = headerText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length > 1)
        {
            return lines.Select(l => l.Trim().Trim('"', '\'')).Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
        }

        string singleLine = headerText.Trim();

        // Detect delimiter in single line: comma, tab, pipe, semicolon
        char? delim = defaultDelimiter;
        if (delim == null)
        {
            if (singleLine.Contains('\t')) delim = '\t';
            else if (singleLine.Contains('|')) delim = '|';
            else if (singleLine.Contains(',')) delim = ',';
            else if (singleLine.Contains(';')) delim = ';';
        }

        if (delim != null)
        {
            var records = ParseCsvRecords(singleLine, delim.Value);
            if (records.Count > 0 && records[0].Count > 0)
            {
                var list = records[0].Select(c => c.Trim().Trim('"', '\'')).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
                if (list.Count > 0) return list;
            }
        }

        // Fallback: split on whitespace
        var whitespaceParts = singleLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                                        .Select(s => s.Trim().Trim('"', '\''))
                                        .Where(s => !string.IsNullOrWhiteSpace(s))
                                        .ToList();
        return whitespaceParts.Count > 0 ? whitespaceParts : new List<string> { singleLine };
    }

    /// <summary>
    /// Generates a list of default surrogate header names (e.g. Column_1, Column_2 or Col1, Col2).
    /// </summary>
    public static List<string> GenerateSurrogateHeaders(int count, string prefix = "Column_")
    {
        if (count <= 0) count = 1;
        return Enumerable.Range(1, count).Select(i => $"{prefix}{i}").ToList();
    }

    public static bool DetectHasHeaders(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (rows == null || rows.Count == 0) return false;

        if (rows.Count == 1)
        {
            var singleRow = rows[0];
            if (singleRow.Count == 0) return false;
            if (singleRow.Any(c => IsNumericCell(c) || IsDateCell(c) || IsGuidCell(c) || IsUrlCell(c) || IsEmailCell(c)))
            {
                return false;
            }
            int keywordCount = singleRow.Count(IsHeaderKeyword);
            return keywordCount > 0 && keywordCount >= Math.Max(1, singleRow.Count / 2);
        }

        var row0 = rows[0];
        if (row0.Count == 0) return false;

        var dataRows = rows.Skip(1).Take(20).ToList();
        int maxCols = rows.Take(21).Max(r => r.Count);

        int headerPoints = 0;
        int dataPoints = 0;

        // Check for duplicate non-empty values in row 0 (headers are rarely duplicates)
        var row0NonEmpty = row0.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        if (row0NonEmpty.Count > 1 && row0NonEmpty.Distinct(StringComparer.OrdinalIgnoreCase).Count() < row0NonEmpty.Count)
        {
            dataPoints += 4;
        }

        for (int c = 0; c < maxCols; c++)
        {
            string hCell = c < row0.Count ? row0[c].Trim() : string.Empty;
            var dataCells = dataRows.Select(r => c < r.Count ? r[c].Trim() : string.Empty)
                                    .Where(s => !string.IsNullOrEmpty(s))
                                    .ToList();

            bool hIsNum = IsNumericCell(hCell);
            bool hIsDate = IsDateCell(hCell);
            bool hIsGuid = IsGuidCell(hCell);
            bool hIsUrl = IsUrlCell(hCell);
            bool hIsEmail = IsEmailCell(hCell);
            bool hIsKeyword = IsHeaderKeyword(hCell);

            if (hIsKeyword) headerPoints += 3;

            if (hIsNum) dataPoints += 4;
            if (hIsGuid) dataPoints += 5;
            if (hIsDate) dataPoints += 4;
            if (hIsUrl) dataPoints += 3;
            if (hIsEmail) dataPoints += 3;

            if (dataCells.Count > 0)
            {
                int numDataNum = dataCells.Count(IsNumericCell);
                int numDataDate = dataCells.Count(IsDateCell);
                int numDataGuid = dataCells.Count(IsGuidCell);
                int numDataUrl = dataCells.Count(IsUrlCell);
                int numDataEmail = dataCells.Count(IsEmailCell);

                double threshold = Math.Max(1, dataCells.Count * 0.6);

                // Type mismatch: Row 0 is text/keyword, while data rows are strongly typed
                if (!hIsNum && numDataNum >= threshold) headerPoints += 5;
                if (!hIsDate && numDataDate >= threshold) headerPoints += 5;
                if (!hIsGuid && numDataGuid >= threshold) headerPoints += 5;
                if (!hIsUrl && numDataUrl >= threshold) headerPoints += 4;
                if (!hIsEmail && numDataEmail >= threshold) headerPoints += 4;

                // Type match on numeric/guid: Both row 0 and data rows are numbers/guids
                if (hIsNum && numDataNum >= threshold) dataPoints += 3;
                if (hIsGuid && numDataGuid >= threshold) dataPoints += 4;
                if (hIsDate && numDataDate >= threshold) dataPoints += 3;
            }
        }

        if (headerPoints > dataPoints) return true;
        if (dataPoints > headerPoints) return false;

        if (dataPoints > 0) return false;
        return headerPoints > 0;
    }

    private static readonly Regex HeaderPatternRegex = new(@"^(?:col(?:umn)?|field|header|param|prop|attr|item|value|var|val|key)\d*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool IsHeaderKeyword(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (CommonHeaderKeywords.Contains(s)) return true;
        string normalized = Regex.Replace(s, @"[\s_\-\.\/]+", "");
        if (CommonHeaderKeywords.Contains(normalized)) return true;
        if (HeaderPatternRegex.IsMatch(s) || HeaderPatternRegex.IsMatch(normalized)) return true;

        var parts = s.Split(new[] { ' ', '/', '-', '_', '.' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 1 && parts.Any(p => CommonHeaderKeywords.Contains(p) || HeaderPatternRegex.IsMatch(p)))
        {
            return true;
        }

        return false;
    }

    private static bool IsNumericCell(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim('$', '€', '£', '¥', '%', ' ', '\t', '"', '\'');
        if (s.Length == 0 || !s.Any(char.IsDigit)) return false;
        return double.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out _)
            || double.TryParse(s, System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.CurrentCulture, out _)
            || (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) && long.TryParse(s[2..], System.Globalization.NumberStyles.HexNumber, null, out _));
    }

    private static bool IsDateCell(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return DateTime.TryParse(s, out _) && s.Any(char.IsDigit) && (s.Contains('/') || s.Contains('-') || s.Contains(':') || s.Contains('.'));
    }

    private static bool IsGuidCell(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return Guid.TryParse(s, out _);
    }

    private static bool IsUrlCell(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsEmailCell(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return s.Contains('@') && s.Contains('.') && !s.Contains(' ');
    }

    public static char? DetectDelimiter(string text)
    {
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                        .Take(15)
                        .ToList();

        if (lines.Count == 0) return null;

        var scores = new Dictionary<char, double>();

        foreach (var d in CandidateDelimiters)
        {
            var counts = lines.Select(l => CountDelimiterInstances(l, d)).ToList();
            if (counts.All(c => c == 0)) continue;

            int firstCount = counts[0];
            if (firstCount <= 0) continue;

            // Check consistency of delimiter counts across lines
            int consistentCount = counts.Count(c => c == firstCount);
            double consistencyRatio = (double)consistentCount / counts.Count;

            // Score: consistency has high weight, tab has slight preference over comma if equal
            double score = consistencyRatio * 10.0 + (firstCount > 0 ? 2.0 : 0);
            if (d == '\t' && consistencyRatio > 0.8) score += 1.5;
            if (d == ',' && consistencyRatio > 0.8) score += 1.0;

            scores[d] = score;
        }

        if (scores.Count == 0) return null;

        var best = scores.OrderByDescending(kv => kv.Value).First();
        return best.Value > 5.0 ? best.Key : null;
    }

    private static int CountDelimiterInstances(string line, char delimiter)
    {
        int count = 0;
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == delimiter && !inQuotes)
            {
                count++;
            }
        }
        return count;
    }

    public static List<List<string>> ParseCsvRecords(string text, char delimiter)
    {
        var rows = new List<List<string>>();
        var currentRow = new List<string>();
        var currentCell = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < text.Length && text[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    currentRow.Add(currentCell.ToString().Trim());
                    currentCell.Clear();
                }
                else if (c == '\r')
                {
                    if (i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    currentRow.Add(currentCell.ToString().Trim());
                    currentCell.Clear();
                    if (currentRow.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                    {
                        rows.Add(currentRow);
                    }
                    currentRow = new List<string>();
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentCell.ToString().Trim());
                    currentCell.Clear();
                    if (currentRow.Any(cell => !string.IsNullOrWhiteSpace(cell)))
                    {
                        rows.Add(currentRow);
                    }
                    currentRow = new List<string>();
                }
                else
                {
                    currentCell.Append(c);
                }
            }
        }

        if (currentCell.Length > 0 || currentRow.Count > 0)
        {
            currentRow.Add(currentCell.ToString().Trim());
            if (currentRow.Any(cell => !string.IsNullOrWhiteSpace(cell)))
            {
                rows.Add(currentRow);
            }
        }

        return rows;
    }

    public static TabularData? TryParseJsonArray(string text, bool? assumeHeader = null)
    {
        string trimmed = text.Trim();
        if (!trimmed.StartsWith('[') || !trimmed.EndsWith(']')) return null;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return null;

            var items = doc.RootElement.EnumerateArray().ToList();
            if (items.Count == 0) return null;

            if (items.All(x => x.ValueKind == JsonValueKind.Object))
            {
                var headers = new List<string>();
                foreach (var item in items)
                {
                    foreach (var prop in item.EnumerateObject())
                    {
                        if (!headers.Contains(prop.Name))
                        {
                            headers.Add(prop.Name);
                        }
                    }
                }

                var table = new TabularData
                {
                    HasHeaders = true,
                    Columns = headers
                };

                foreach (var item in items)
                {
                    var row = new List<string>();
                    foreach (var header in headers)
                    {
                        if (item.TryGetProperty(header, out var val))
                        {
                            row.Add(val.ToString());
                        }
                        else
                        {
                            row.Add(string.Empty);
                        }
                    }
                    table.Rows.Add(row);
                }

                return table;
            }
            else if (items.All(x => x.ValueKind == JsonValueKind.Array))
            {
                var allRows = new List<List<string>>();
                foreach (var item in items)
                {
                    var row = item.EnumerateArray().Select(elem => elem.ToString()).ToList();
                    allRows.Add(row);
                }

                if (allRows.Count == 0) return null;

                bool hasHeaders = assumeHeader ?? DetectHasHeaders(allRows);
                var table = new TabularData
                {
                    HasHeaders = hasHeaders
                };

                if (hasHeaders)
                {
                    table.Columns = allRows[0];
                    table.Rows = allRows.Skip(1).ToList();
                }
                else
                {
                    int maxCols = allRows.Max(r => r.Count);
                    table.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
                    table.Rows = allRows;
                }

                return table;
            }
        }
        catch
        {
            // not valid JSON array
        }

        return null;
    }

    public static TabularData? TryParseYaml(string text, bool? assumeHeader = null)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        string trimmed = text.Trim();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('<')) return null;
        if (!trimmed.StartsWith('-') && !trimmed.StartsWith("---") && !trimmed.StartsWith('[')) return null;

        try
        {
            var deserializer = new DeserializerBuilder().Build();
            var result = deserializer.Deserialize(new StringReader(trimmed));

            if (result is IList<object> list && list.Count > 0)
            {
                // Case 1: List of dictionaries / maps
                if (list.All(item => item is IDictionary<object, object>))
                {
                    var headers = new List<string>();
                    foreach (var item in list)
                    {
                        if (item is IDictionary<object, object> dict)
                        {
                            foreach (var key in dict.Keys)
                            {
                                string keyStr = key?.ToString() ?? string.Empty;
                                if (!headers.Contains(keyStr))
                                {
                                    headers.Add(keyStr);
                                }
                            }
                        }
                    }

                    if (headers.Count == 0) return null;

                    var table = new TabularData
                    {
                        HasHeaders = true,
                        Columns = headers
                    };

                    foreach (var item in list)
                    {
                        var dict = (IDictionary<object, object>)item;
                        var row = new List<string>();
                        foreach (var header in headers)
                        {
                            object? matchingVal = null;
                            foreach (var kv in dict)
                            {
                                if (string.Equals(kv.Key?.ToString(), header, StringComparison.Ordinal))
                                {
                                    matchingVal = kv.Value;
                                    break;
                                }
                            }
                            row.Add(matchingVal?.ToString() ?? string.Empty);
                        }
                        table.Rows.Add(row);
                    }

                    return table;
                }
                // Case 2: List of lists / sequences
                else if (list.All(item => item is IList<object>))
                {
                    var allRows = new List<List<string>>();
                    foreach (var item in list)
                    {
                        var row = ((IList<object>)item).Select(elem => elem?.ToString() ?? string.Empty).ToList();
                        allRows.Add(row);
                    }

                    if (allRows.Count == 0) return null;

                    bool hasHeaders = assumeHeader ?? DetectHasHeaders(allRows);
                    var table = new TabularData
                    {
                        HasHeaders = hasHeaders
                    };

                    if (hasHeaders)
                    {
                        table.Columns = allRows[0];
                        table.Rows = allRows.Skip(1).ToList();
                    }
                    else
                    {
                        int maxCols = allRows.Max(r => r.Count);
                        table.Columns = Enumerable.Range(1, maxCols).Select(i => $"Column {i}").ToList();
                        table.Rows = allRows;
                    }

                    return table;
                }
            }
        }
        catch
        {
            // Not valid YAML
        }

        return null;
    }
}
