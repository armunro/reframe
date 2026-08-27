using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Reframe.Core.RegexLab;

public static class RegexLibraryCatalog
{
    private static readonly List<RegexPatternPreset> _presets = new()
    {
        new RegexPatternPreset
        {
            Id = "iso-8601-date",
            Name = "ISO 8601 Date & Timestamp",
            Category = "Identifiers & Formats",
            Icon = "📅",
            Pattern = @"\b(?<year>\d{4})-(?<month>0[1-9]|1[0-2])-(?<day>0[1-9]|[12]\d|3[01])(?:[T ](?<hour>[01]\d|2[0-3]):(?<minute>[0-5]\d):(?<second>[0-5]\d)(?:\.(?<ms>\d{1,9}))?(?<tz>Z|[+-][01]\d:?[0-5]\d)?)?\b",
            Description = "Matches ISO 8601 dates and full timestamps with optional millisecond precision and UTC/timezone offset.",
            SampleText = "Events: 2026-08-26T18:46:00.000Z and 2024-12-31 23:59:59+00:00 or simple date 2025-01-15.",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "email-address",
            Name = "Email Address (RFC 5322 Standard)",
            Category = "Web & Network",
            Icon = "✉️",
            Pattern = @"\b(?<user>[a-zA-Z0-9._%+-]+)@(?<domain>[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b",
            Description = "Captures local mailbox usernames and domain names from email addresses.",
            SampleText = "Contact us at developer@example.com, john.doe+test@sub.domain.co.uk, or support@github.com.",
            DefaultOptions = RegexOptions.IgnoreCase
        },
        new RegexPatternPreset
        {
            Id = "semver",
            Name = "Semantic Versioning (SemVer 2.0)",
            Category = "Development",
            Icon = "🏷️",
            Pattern = @"\bv?(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?\b",
            Description = "Extracts major, minor, patch, prerelease tags, and build metadata per SemVer 2.0.0 specification.",
            SampleText = "Release versions: v1.0.0, 2.1.3-alpha.1, 3.0.0-beta.2+20260826, 0.9.12.",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "uuid-guid",
            Name = "UUID / GUID",
            Category = "Identifiers & Formats",
            Icon = "🔑",
            Pattern = @"\b(?<guid>[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[1-8][0-9a-fA-F]{3}-[89abAB][0-9a-fA-F]{3}-[0-9a-fA-F]{12})\b",
            Description = "Matches standard 8-4-4-4-12 hex UUID/GUID format strings across versions 1 through 8.",
            SampleText = "IDs: e029b8b2-3d77-4b68-b7a4-098e94be7882 and 550e8400-e29b-41d4-a716-446655440000.",
            DefaultOptions = RegexOptions.IgnoreCase
        },
        new RegexPatternPreset
        {
            Id = "jwt-token",
            Name = "JWT Token (JSON Web Token)",
            Category = "Security & Auth",
            Icon = "🛡️",
            Pattern = @"\b(?<header>eyJ[a-zA-Z0-9_-]+)\.(?<payload>eyJ[a-zA-Z0-9_-]+)\.(?<signature>[a-zA-Z0-9_-]+)\b",
            Description = "Extracts Base64Url-encoded Header, Payload, and Signature components of JSON Web Tokens.",
            SampleText = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "ipv4-address",
            Name = "IPv4 Address",
            Category = "Web & Network",
            Icon = "🌐",
            Pattern = @"\b(?<octet1>25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.(?<octet2>25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.(?<octet3>25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\.(?<octet4>25[0-5]|2[0-4]\d|1\d\d|[1-9]?\d)\b",
            Description = "Validates and extracts all 4 octets of standard dotted decimal IPv4 network addresses (0..255).",
            SampleText = "Host IPs: 192.168.1.1, gateway 10.0.0.254, loopback 127.0.0.1, subnet mask 255.255.255.0.",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "ipv6-address",
            Name = "IPv6 Address",
            Category = "Web & Network",
            Icon = "🌐",
            Pattern = @"\b(?:(?:[0-9a-fA-F]{1,4}:){7}[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,7}:|(?:[0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|(?:[0-9a-fA-F]{1,4}:){1,5}(?::[0-9a-fA-F]{1,4}){1,2}|(?:[0-9a-fA-F]{1,4}:){1,4}(?::[0-9a-fA-F]{1,4}){1,3}|(?:[0-9a-fA-F]{1,4}:){1,3}(?::[0-9a-fA-F]{1,4}){1,4}|(?:[0-9a-fA-F]{1,4}:){1,2}(?::[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:(?::[0-9a-fA-F]{1,4}){1,6}|:(?:(?::[0-9a-fA-F]{1,4}){1,7}|:))\b",
            Description = "Matches full, compressed (::), and dual IPv6 addresses.",
            SampleText = "IPv6 servers: 2001:0db8:85a3:0000:0000:8a2e:0370:7334, fe80::1, ::1.",
            DefaultOptions = RegexOptions.IgnoreCase
        },
        new RegexPatternPreset
        {
            Id = "connection-string",
            Name = "Database Connection String (Key-Value)",
            Category = "Databases & Config",
            Icon = "🗄️",
            Pattern = @"(?<key>[a-zA-Z0-9_\s.-]+)\s*=\s*(?<value>""[^""]*""|'[^']*'|[^;]*)(?:;|\s*$)",
            Description = "Extracts property keys and values from SQL Server, PostgreSQL, MySQL, and generic connection strings.",
            SampleText = "Server=tcp:sqlserver.database.windows.net,1433;Database=MyInventoryDb;User Id=dbadmin;Password=\"Secret!P@ss\";Encrypt=True;TrustServerCertificate=False;",
            DefaultOptions = RegexOptions.Multiline
        },
        new RegexPatternPreset
        {
            Id = "url-http-https",
            Name = "URL / URI (HTTP & HTTPS)",
            Category = "Web & Network",
            Icon = "🔗",
            Pattern = @"\b(?<protocol>https?|ftp):\/\/(?<domain>[a-zA-Z0-9.-]+)(?::(?<port>\d+))?(?<path>\/[^\s?#]*)?(?:\?(?<query>[^\s#]*))?(?:#(?<fragment>[^\s]*))?",
            Description = "Captures protocol, domain, port, path, query string, and hash fragment of web URLs.",
            SampleText = "API endpoint: https://api.example.com:8080/v1/users?sort=asc&limit=10#section1 and http://localhost:3000/test",
            DefaultOptions = RegexOptions.IgnoreCase
        },
        new RegexPatternPreset
        {
            Id = "hex-color",
            Name = "Hex Color Code (CSS / Hex)",
            Category = "Design & UI",
            Icon = "🎨",
            Pattern = @"#(?:(?<alpha>[0-9a-fA-F]{2})?(?<red>[0-9a-fA-F]{2})(?<green>[0-9a-fA-F]{2})(?<blue>[0-9a-fA-F]{2})|(?<shortRed>[0-9a-fA-F])(?<shortGreen>[0-9a-fA-F])(?<shortBlue>[0-9a-fA-F]))\b",
            Description = "Matches 3-digit, 6-digit, and 8-digit hexadecimal color codes (#RGB, #RRGGBB, #AARRGGBB).",
            SampleText = "Palette: primary #FF5733, accent #33A8FF, background #FFFFFF, dark #000, shorthand #F0A, translucent #80FF00AA.",
            DefaultOptions = RegexOptions.IgnoreCase
        },
        new RegexPatternPreset
        {
            Id = "phone-number",
            Name = "Phone Number (US / International)",
            Category = "Identifiers & Formats",
            Icon = "📞",
            Pattern = @"(?:\+?(?<country>\d{1,3})[-. ]?)?\(?(?<area>\d{3})\)?[-. ]?(?<exchange>\d{3})[-. ]?(?<number>\d{4})\b",
            Description = "Extracts country code, area code, exchange, and subscriber number from various phone formats.",
            SampleText = "Call lines: +1 (555) 123-4567, 555-867-5309, 800.555.0199, or +44 207 123 4567.",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "markdown-links",
            Name = "Markdown Links [Text](URL)",
            Category = "Content & Text",
            Icon = "📝",
            Pattern = @"\[(?<text>[^\]]+)\]\((?<url>[^)\s]+)(?:\s+""(?<title>[^""]*)"")?\)",
            Description = "Extracts anchor text, target URL, and optional title from Markdown formatted hyperlinks.",
            SampleText = "Check out [JetBrains](https://jetbrains.com \"Leading IDEs\") and [Reframe](https://github.com/reframe).",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "key-value-query",
            Name = "Key-Value Query String Parameters",
            Category = "Web & Network",
            Icon = "🔍",
            Pattern = @"(?<key>[a-zA-Z0-9_.-]+)=(?<value>[^&;\s]+)",
            Description = "Extracts key-value query parameters from URL query strings or form urlencoded payloads.",
            SampleText = "userId=123&action=export&format=json&enabled=true&filter=active",
            DefaultOptions = RegexOptions.None
        },
        new RegexPatternPreset
        {
            Id = "html-xml-tags",
            Name = "HTML / XML Tags & Attributes",
            Category = "Development",
            Icon = "🏷️",
            Pattern = @"<(?<tag>[a-zA-Z0-9:-]+)(?<attributes>[^>]*)>(?<content>.*?)<\/\k<tag>>|<(?<selfClosingTag>[a-zA-Z0-9:-]+)(?<selfAttributes>[^>]*)\/>",
            Description = "Matches opening, closing, and self-closing HTML or XML tags with attributes and inner content.",
            SampleText = "<div class=\"container\" id=\"main\"><p>Hello World</p><img src=\"logo.png\" alt=\"Logo\"/></div>",
            DefaultOptions = RegexOptions.Singleline
        }
    };

    public static IReadOnlyList<RegexPatternPreset> Presets => _presets;

    public static RegexPatternPreset? FindById(string id)
    {
        return _presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase));
    }

    public static IEnumerable<RegexPatternPreset> GetByCategory(string category)
    {
        return _presets.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
    }
}
