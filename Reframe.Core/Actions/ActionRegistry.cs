using System.Collections.Generic;

namespace Reframe.Core.Actions;

public static class ActionRegistry
{
    private static readonly List<ActionItem> _allActions = new()
    {
        // -------------------------------------------------------------
        // Lines Operations (Tab 0)
        // -------------------------------------------------------------
        new ActionItem(
            id: "JoinLines",
            title: "Join Lines into Single Row",
            category: "Lines",
            description: "Join multiple lines into a single line with custom delimiters and quotes",
            keywords: ["join", "combine", "merge", "concat", "single line", "delimiter", "comma"],
            icon: "🔗",
            targetSidebarTab: 0),

        new ActionItem(
            id: "QuoteLines",
            title: "Quote All Lines",
            category: "Lines",
            description: "Wrap all lines in single quotes, double quotes, backticks, or brackets",
            keywords: ["quote", "wrap", "single quote", "double quote", "backtick", "brackets", "parens"],
            icon: "💬",
            targetSidebarTab: 0),

        new ActionItem(
            id: "SplitLine",
            title: "Split Delimited Line",
            category: "Lines",
            description: "Split delimited text or regex pattern into multiple lines",
            keywords: ["split", "delimit", "csv to lines", "tokenize", "break lines", "explode"],
            icon: "✂️",
            targetSidebarTab: 0),

        new ActionItem(
            id: "PrefixSuffix",
            title: "Add Prefix / Suffix",
            category: "Lines",
            description: "Prepend prefix and/or append suffix to all lines",
            keywords: ["prefix", "suffix", "prepend", "append", "surround", "add to start", "add to end"],
            icon: "➕",
            targetSidebarTab: 0),

        new ActionItem(
            id: "ReplaceInLines",
            title: "Find & Replace in Lines",
            category: "Lines",
            description: "Search and replace text or regex patterns across lines",
            keywords: ["replace", "find", "search", "substitute", "regex"],
            icon: "🔍",
            targetSidebarTab: 0),

        new ActionItem(
            id: "TrimLines",
            title: "Trim & Clean Lines",
            category: "Lines",
            description: "Trim whitespace, collapse spaces, and remove empty lines",
            keywords: ["trim", "clean", "strip", "whitespace", "empty lines", "collapse spaces"],
            icon: "🧹",
            targetSidebarTab: 0),

        new ActionItem(
            id: "SortLines",
            title: "Sort Lines (Natural / Custom)",
            category: "Lines",
            description: "Sort lines naturally, alphabetically, by length, or reversed",
            keywords: ["sort", "order", "alphabetical", "natural numeric", "ascending", "descending", "reverse"],
            icon: "🔀",
            targetSidebarTab: 0),

        new ActionItem(
            id: "SortAlphabetical",
            title: "Sort Lines Alphabetically (A ➔ Z)",
            category: "Lines",
            description: "Sort lines alphabetically in ascending order",
            keywords: ["sort alphabetical", "sort a-z", "abc"],
            icon: "🔤",
            targetSidebarTab: 0),

        new ActionItem(
            id: "SortNatural",
            title: "Sort Lines Naturally (1, 2, 10)",
            category: "Lines",
            description: "Sort lines with natural numeric ordering",
            keywords: ["sort natural", "natural sort", "numeric sort"],
            icon: "🔢",
            targetSidebarTab: 0),

        new ActionItem(
            id: "Deduplicate",
            title: "Deduplicate Lines (Distinct)",
            category: "Lines",
            description: "Remove duplicate lines or filter for duplicates only",
            keywords: ["deduplicate", "distinct", "unique", "duplicates", "remove dupes", "dedup"],
            icon: "✨",
            targetSidebarTab: 0),

        new ActionItem(
            id: "NumberLines",
            title: "Number Lines",
            category: "Lines",
            description: "Add line numbering with custom format and start index",
            keywords: ["number", "line number", "index", "counter", "enumerate"],
            icon: "🔢",
            targetSidebarTab: 0),

        new ActionItem(
            id: "FilterLines",
            title: "Filter Lines",
            category: "Lines",
            description: "Keep or remove lines matching text or regular expression",
            keywords: ["filter", "grep", "match", "exclude", "include", "regex filter"],
            icon: "🎯",
            targetSidebarTab: 0),

        new ActionItem(
            id: "RegexExtract",
            title: "Regex Extract",
            category: "Lines",
            description: "Extract matching regex patterns or capture groups into lines",
            keywords: ["regex", "extract", "capture", "pattern", "matches", "regexp"],
            icon: "⚡",
            targetSidebarTab: 0),

        // -------------------------------------------------------------
        // Tabular & Table Conversions (Tab 1)
        // -------------------------------------------------------------
        new ActionItem(
            id: "ToCsv",
            title: "Convert to CSV",
            category: "Tabular",
            description: "Convert tabular dataset or markdown/html table into CSV",
            keywords: ["csv", "comma separated", "export csv", "table to csv"],
            icon: "📊",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToTsv",
            title: "Convert to TSV",
            category: "Tabular",
            description: "Convert tabular dataset or table into tab-separated values",
            keywords: ["tsv", "tab separated", "excel", "table to tsv"],
            icon: "📑",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToMarkdownTable",
            title: "Convert to Markdown Table",
            category: "Tabular",
            description: "Convert data into aligned Markdown table with headers",
            keywords: ["markdown", "md table", "pipes", "table to markdown", "gfm"],
            icon: "📋",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToHtmlTable",
            title: "Convert to HTML Table",
            category: "Tabular",
            description: "Convert tabular data into clean HTML <table> structure",
            keywords: ["html", "table", "html table", "web table", "confluence"],
            icon: "🌐",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToJsonObjects",
            title: "Convert to JSON Array of Objects",
            category: "Tabular",
            description: "Convert table rows into JSON array of keyed objects",
            keywords: ["json", "json objects", "table to json", "array of objects", "records"],
            icon: "📦",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToJsonArrays",
            title: "Convert to JSON Array of Arrays",
            category: "Tabular",
            description: "Convert table rows into 2D JSON array of value arrays",
            keywords: ["json arrays", "2d array", "matrix", "table to array"],
            icon: "📦",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToYaml",
            title: "Convert to YAML Array of Objects",
            category: "Tabular",
            description: "Convert table rows into YAML array of key-value objects",
            keywords: ["yaml", "table to yaml", "yaml objects", "yml"],
            icon: "📄",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToYamlArrays",
            title: "Convert to YAML Sequences",
            category: "Tabular",
            description: "Convert table rows into 2D YAML sequences",
            keywords: ["yaml sequences", "yaml arrays", "table to yaml"],
            icon: "📄",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ToSqlInserts",
            title: "Generate SQL INSERTs",
            category: "Tabular",
            description: "Generate SQL INSERT INTO statements from table data",
            keywords: ["sql", "insert", "sql inserts", "database", "insert into", "dml", "table"],
            icon: "🗄️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "TransposeTable",
            title: "Transpose Table (Swap Rows & Cols)",
            category: "Tabular",
            description: "Flip table rows and columns (transpose grid)",
            keywords: ["transpose", "swap", "rotate", "pivot", "rows to columns", "flip table"],
            icon: "🔄",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractColumn",
            title: "Extract Selected Column",
            category: "Tabular",
            description: "Extract single selected column data into lines",
            keywords: ["extract column", "single column", "column values"],
            icon: "📑",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToCsv",
            title: "Selected Columns ➔ CSV",
            category: "Tabular",
            description: "Extract checked columns into CSV format",
            keywords: ["extract csv", "selected columns csv"],
            icon: "📊",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToTsv",
            title: "Selected Columns ➔ TSV",
            category: "Tabular",
            description: "Extract checked columns into TSV format",
            keywords: ["extract tsv", "selected columns tsv"],
            icon: "📑",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToMarkdown",
            title: "Selected Columns ➔ Markdown",
            category: "Tabular",
            description: "Extract checked columns into Markdown table",
            keywords: ["extract markdown", "selected columns md"],
            icon: "📋",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToJson",
            title: "Selected Columns ➔ JSON",
            category: "Tabular",
            description: "Extract checked columns into JSON objects",
            keywords: ["extract json", "selected columns json"],
            icon: "📦",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToYaml",
            title: "Selected Columns ➔ YAML",
            category: "Tabular",
            description: "Extract checked columns into YAML",
            keywords: ["extract yaml", "selected columns yaml"],
            icon: "📄",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToLines",
            title: "Selected Columns ➔ Delimited Lines",
            category: "Tabular",
            description: "Extract checked columns as joined lines",
            keywords: ["extract lines", "selected columns lines"],
            icon: "📐",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToSqlIn",
            title: "Selected Columns ➔ SQL IN",
            category: "Tabular",
            description: "Extract checked columns directly into SQL IN clause",
            keywords: ["extract sql in", "selected columns sql in", "sql clause"],
            icon: "🗄️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ExtractSelectedToCodeArray",
            title: "Selected Columns ➔ Code Array",
            category: "Tabular",
            description: "Extract checked columns into C#/TS/Python array",
            keywords: ["extract code array", "selected columns code array"],
            icon: "💻",
            targetSidebarTab: 1),

        new ActionItem(
            id: "TableToKeyValueJson",
            title: "Table ➔ Key-Value JSON Map",
            category: "Tabular",
            description: "Map Key Column to Value Column as JSON dictionary object",
            keywords: ["table to kv json", "map", "dictionary", "key value json"],
            icon: "🗺️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "TableToKeyValueYaml",
            title: "Table ➔ Key-Value YAML Map",
            category: "Tabular",
            description: "Map Key Column to Value Column as YAML dictionary mapping",
            keywords: ["table to kv yaml", "map", "yaml map", "key value yaml"],
            icon: "🗺️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "TableToKeyValueQuery",
            title: "Table ➔ Key-Value URL Query",
            category: "Tabular",
            description: "Map Key Column to Value Column as URL query string",
            keywords: ["table to query string", "query string", "key value query"],
            icon: "🗺️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "GenerateSurrogateHeaders",
            title: "Generate Surrogate Headers",
            category: "Tabular",
            description: "Auto-generate sequential Col1, Col2... header names",
            keywords: ["surrogate headers", "generate headers", "auto headers", "col1 col2"],
            icon: "🏷️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ApplySurrogateHeaders",
            title: "Apply Surrogate Headers",
            category: "Tabular",
            description: "Apply custom surrogate headers to table",
            keywords: ["apply headers", "override headers", "custom headers"],
            icon: "🏷️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ClearSurrogateHeaders",
            title: "Clear Surrogate Headers",
            category: "Tabular",
            description: "Reset custom surrogate headers",
            keywords: ["clear headers", "reset headers"],
            icon: "🧹",
            targetSidebarTab: 1),

        new ActionItem(
            id: "KeepOnlySelectedColumns",
            title: "Keep Only Selected Columns",
            category: "Tabular",
            description: "Remove unchecked columns from the current table",
            keywords: ["keep columns", "pick columns", "select columns"],
            icon: "✂️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "DropSelectedColumns",
            title: "Drop Selected Columns",
            category: "Tabular",
            description: "Remove checked columns from the current table",
            keywords: ["drop columns", "remove columns", "delete columns"],
            icon: "🗑️",
            targetSidebarTab: 1),

        new ActionItem(
            id: "SortTableByColumn",
            title: "Sort Table by Column",
            category: "Tabular",
            description: "Sort entire tabular dataset by selected column",
            keywords: ["sort table", "order by column", "sort by column"],
            icon: "🔀",
            targetSidebarTab: 1),

        new ActionItem(
            id: "FilterTableByColumn",
            title: "Filter Table by Column",
            category: "Tabular",
            description: "Filter rows where selected column matches query",
            keywords: ["filter table", "where column", "filter rows"],
            icon: "🎯",
            targetSidebarTab: 1),

        // -------------------------------------------------------------
        // Structured Data: JSON / YAML / XML (Tab 2)
        // -------------------------------------------------------------
        new ActionItem(
            id: "FormatJson",
            title: "Format / Beautify JSON",
            category: "Structured",
            description: "Format, indent, and prettify JSON text",
            keywords: ["format json", "beautify json", "prettify json", "indent json", "pretty json"],
            icon: "✨",
            targetSidebarTab: 2),

        new ActionItem(
            id: "FormatXml",
            title: "Format / Beautify XML",
            category: "Structured",
            description: "Format, indent, and prettify XML documents",
            keywords: ["format xml", "beautify xml", "prettify xml", "indent xml"],
            icon: "✨",
            targetSidebarTab: 2),

        new ActionItem(
            id: "FormatYaml",
            title: "Format / Beautify YAML",
            category: "Structured",
            description: "Format, indent, and clean YAML documents",
            keywords: ["format yaml", "beautify yaml", "prettify yaml", "indent yaml"],
            icon: "✨",
            targetSidebarTab: 2),

        new ActionItem(
            id: "MinifyJson",
            title: "Minify JSON",
            category: "Structured",
            description: "Strip whitespace and newlines for compact JSON payload",
            keywords: ["minify json", "compact json", "compress json", "strip json"],
            icon: "🗜️",
            targetSidebarTab: 2),

        new ActionItem(
            id: "MinifyXml",
            title: "Minify XML",
            category: "Structured",
            description: "Strip whitespace and newlines for compact XML document",
            keywords: ["minify xml", "compact xml", "compress xml", "strip xml"],
            icon: "🗜️",
            targetSidebarTab: 2),

        new ActionItem(
            id: "JsonToYaml",
            title: "JSON ➔ YAML",
            category: "Structured",
            description: "Convert JSON document to formatted YAML",
            keywords: ["json to yaml", "json2yaml", "json yaml converter", "yml"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "YamlToJson",
            title: "YAML ➔ JSON",
            category: "Structured",
            description: "Convert YAML document to formatted JSON",
            keywords: ["yaml to json", "yaml2json", "yaml json converter"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "XmlToJson",
            title: "XML ➔ JSON",
            category: "Structured",
            description: "Convert XML document to structured JSON (preserves @attributes)",
            keywords: ["xml to json", "xml2json", "convert xml"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "JsonToXml",
            title: "JSON ➔ XML",
            category: "Structured",
            description: "Convert JSON document to structured XML",
            keywords: ["json to xml", "json2xml", "convert json to xml"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "XmlToYaml",
            title: "XML ➔ YAML",
            category: "Structured",
            description: "Convert XML document to formatted YAML",
            keywords: ["xml to yaml", "xml2yaml"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "YamlToXml",
            title: "YAML ➔ XML",
            category: "Structured",
            description: "Convert YAML document to structured XML",
            keywords: ["yaml to xml", "yaml2xml"],
            icon: "🔄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredToCsv",
            title: "Structured ➔ CSV",
            category: "Structured",
            description: "Convert JSON/YAML/XML records into CSV format",
            keywords: ["structured to csv", "json to csv", "xml to csv", "yaml to csv"],
            icon: "📊",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredToTsv",
            title: "Structured ➔ TSV",
            category: "Structured",
            description: "Convert JSON/YAML/XML records into TSV format",
            keywords: ["structured to tsv", "json to tsv", "xml to tsv", "yaml to tsv"],
            icon: "📑",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredToMarkdown",
            title: "Structured ➔ Markdown Table",
            category: "Structured",
            description: "Convert JSON/YAML/XML records into Markdown table",
            keywords: ["structured to markdown", "json to markdown", "json to table"],
            icon: "📋",
            targetSidebarTab: 2),

        new ActionItem(
            id: "FlattenStructured",
            title: "Flatten to Paths",
            category: "Structured",
            description: "Flatten nested structure to dot-notation / bracket paths (a.b[0].c = val)",
            keywords: ["flatten", "dot notation", "paths", "flatten json", "flatten xml"],
            icon: "🌲",
            targetSidebarTab: 2),

        new ActionItem(
            id: "FlattenToFlatJson",
            title: "Flatten to Flat JSON",
            category: "Structured",
            description: "Flatten nested structure to flat JSON object with dot-keys",
            keywords: ["flatten json", "flat json", "dot keys"],
            icon: "📦",
            targetSidebarTab: 2),

        new ActionItem(
            id: "UnflattenToJson",
            title: "Unflatten Paths ➔ JSON",
            category: "Structured",
            description: "Reconstruct nested JSON structure from dot-notation paths",
            keywords: ["unflatten", "unflatten json", "expand paths", "reconstruct"],
            icon: "🌳",
            targetSidebarTab: 2),

        new ActionItem(
            id: "UnflattenToYaml",
            title: "Unflatten Paths ➔ YAML",
            category: "Structured",
            description: "Reconstruct nested YAML structure from dot-notation paths",
            keywords: ["unflatten yaml", "expand paths yaml"],
            icon: "🌳",
            targetSidebarTab: 2),

        new ActionItem(
            id: "SortStructuredKeysAsc",
            title: "Deep Sort Keys (A ➔ Z)",
            category: "Structured",
            description: "Recursively sort all object/mapping keys alphabetically ascending",
            keywords: ["sort keys", "deep sort", "alphabetical keys", "sort json keys", "sort yaml keys"],
            icon: "🔤",
            targetSidebarTab: 2),

        new ActionItem(
            id: "SortStructuredKeysDesc",
            title: "Deep Sort Keys (Z ➔ A)",
            category: "Structured",
            description: "Recursively sort all object/mapping keys alphabetically descending",
            keywords: ["sort keys desc", "deep sort desc", "reverse sort keys"],
            icon: "🔤",
            targetSidebarTab: 2),

        new ActionItem(
            id: "QueryStructuredPath",
            title: "Query JSONPath / Property",
            category: "Structured",
            description: "Evaluate JSONPath expression (e.g. $.store.books[*].title)",
            keywords: ["jsonpath", "query json", "json selector", "extract jsonpath"],
            icon: "🔍",
            targetSidebarTab: 2),

        new ActionItem(
            id: "QueryXPath",
            title: "Query XPath Expression",
            category: "Structured",
            description: "Evaluate full XPath query across XML/JSON/YAML documents",
            keywords: ["xpath", "xml query", "xpath expression", "xpath filter", "query xml"],
            icon: "🔍",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ExtractXPathValues",
            title: "Extract XPath Values",
            category: "Structured",
            description: "Extract matched XPath inner values/text into clean line list",
            keywords: ["xpath values", "extract xpath", "xpath text"],
            icon: "📝",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ExtractXPathAttributes",
            title: "Extract XPath Attributes",
            category: "Structured",
            description: "Extract matched XPath attributes as @attr=\"value\" lines",
            keywords: ["xpath attributes", "extract attributes", "@attr"],
            icon: "🏷️",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ExtractStructuredPaths",
            title: "Extract All Distinct Paths",
            category: "Structured",
            description: "Extract list of all distinct paths/JSONPaths across document",
            keywords: ["all paths", "extract paths", "json paths list"],
            icon: "🗺️",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ExtractStructuredKeys",
            title: "Extract All Unique Keys",
            category: "Structured",
            description: "Extract list of all unique property and element names",
            keywords: ["all keys", "extract keys", "property names", "unique keys"],
            icon: "🔑",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ExtractStructuredValues",
            title: "Extract All Scalar Values",
            category: "Structured",
            description: "Extract list of all scalar values into lines",
            keywords: ["all values", "extract values", "scalar values"],
            icon: "📄",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredCamelCase",
            title: "Structured Keys ➔ camelCase",
            category: "Structured",
            description: "Recursively convert all object property keys to camelCase",
            keywords: ["camelcase keys", "json camelcase", "casing keys"],
            icon: "🔡",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredPascalCase",
            title: "Structured Keys ➔ PascalCase",
            category: "Structured",
            description: "Recursively convert all object property keys to PascalCase",
            keywords: ["pascalcase keys", "json pascalcase"],
            icon: "🔠",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredSnakeCase",
            title: "Structured Keys ➔ snake_case",
            category: "Structured",
            description: "Recursively convert all object property keys to snake_case",
            keywords: ["snakecase keys", "json snakecase"],
            icon: "🐍",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredKebabCase",
            title: "Structured Keys ➔ kebab-case",
            category: "Structured",
            description: "Recursively convert all object property keys to kebab-case",
            keywords: ["kebabcase keys", "json kebabcase"],
            icon: "🍢",
            targetSidebarTab: 2),

        new ActionItem(
            id: "StructuredConstantCase",
            title: "Structured Keys ➔ CONSTANT_CASE",
            category: "Structured",
            description: "Recursively convert all object property keys to CONSTANT_CASE",
            keywords: ["constantcase keys", "screaming snake keys"],
            icon: "📢",
            targetSidebarTab: 2),

        new ActionItem(
            id: "PickStructuredKeys",
            title: "Pick / Keep Keys",
            category: "Structured",
            description: "Keep only specified keys across nested objects",
            keywords: ["pick keys", "keep keys", "whitelist keys", "filter keys"],
            icon: "🎯",
            targetSidebarTab: 2),

        new ActionItem(
            id: "OmitStructuredKeys",
            title: "Omit / Remove Keys",
            category: "Structured",
            description: "Strip specified sensitive or unwanted keys across objects",
            keywords: ["omit keys", "remove keys", "blacklist keys", "strip keys", "censor"],
            icon: "🚫",
            targetSidebarTab: 2),

        new ActionItem(
            id: "RemoveNullsAndEmpty",
            title: "Remove Nulls & Empty Fields",
            category: "Structured",
            description: "Recursively strip null values, empty strings, and empty objects",
            keywords: ["remove nulls", "strip null", "clean json", "remove empty"],
            icon: "🧹",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ToTypeScriptInterfaces",
            title: "Generate TypeScript Interfaces",
            category: "Structured",
            description: "Infer typed TypeScript interface definitions from JSON/YAML",
            keywords: ["typescript", "ts", "interface", "type", "d.ts", "types", "generate ts"],
            icon: "🟦",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ToCSharpClasses",
            title: "Generate C# POCO Classes",
            category: "Structured",
            description: "Infer typed C# class definitions with properties from JSON/YAML",
            keywords: ["c#", "csharp", "poco", "class", "dto", "model", "generate c#", "c# classes"],
            icon: "🟩",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ToJsonSchema",
            title: "Generate JSON Schema",
            category: "Structured",
            description: "Infer draft-07 JSON Schema definition from structured data",
            keywords: ["json schema", "schema", "validation", "draft-07"],
            icon: "📐",
            targetSidebarTab: 2),

        // -------------------------------------------------------------
        // Code & Developer Tools (Tab 3)
        // -------------------------------------------------------------
        new ActionItem(
            id: "SqlIn",
            title: "SQL IN (...) Clause",
            category: "Code & Developer",
            description: "Format items into single-line SQL IN ('a', 'b', 'c') clause",
            keywords: ["sql", "in", "sql in", "where in", "sql clause", "database query"],
            icon: "🗄️",
            targetSidebarTab: 3),

        new ActionItem(
            id: "SqlInMultiLine",
            title: "SQL IN Multi-line Clause",
            category: "Code & Developer",
            description: "Format items into indented multi-line SQL IN clause",
            keywords: ["sql in multiline", "sql multiline", "where in multi"],
            icon: "🗄️",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToCSharpArray",
            title: "Generate C# string[] Array",
            category: "Code & Developer",
            description: "Convert lines into C# new string[] { ... } literal",
            keywords: ["c# array", "csharp array", "string array", "code array"],
            icon: "💻",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToCSharpList",
            title: "Generate C# List<string>",
            category: "Code & Developer",
            description: "Convert lines into C# new List<string> { ... } literal",
            keywords: ["c# list", "csharp list", "list string"],
            icon: "💻",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToTypeScriptArray",
            title: "Generate TypeScript Array",
            category: "Code & Developer",
            description: "Convert lines into TypeScript const items: string[] = [...]",
            keywords: ["typescript array", "ts array", "js array", "javascript array"],
            icon: "💻",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToPythonList",
            title: "Generate Python List",
            category: "Code & Developer",
            description: "Convert lines into Python items = [...] list literal",
            keywords: ["python list", "py list", "python array"],
            icon: "💻",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToJsonArray",
            title: "Generate JSON Array",
            category: "Code & Developer",
            description: "Convert lines into standard JSON [\"a\", \"b\"] array",
            keywords: ["json array", "json list", "to json array"],
            icon: "📦",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ToYamlArray",
            title: "Generate YAML Sequence",
            category: "Code & Developer",
            description: "Convert lines into YAML - item sequence list",
            keywords: ["yaml list", "yaml sequence", "to yaml array", "yaml array"],
            icon: "📄",
            targetSidebarTab: 3),

        new ActionItem(
            id: "QueryStringToKv",
            title: "URL Query ➔ Key-Values",
            category: "Code & Developer",
            description: "Parse URL query string parameters into key=value lines",
            keywords: ["query string to kv", "url params", "query params", "decode query"],
            icon: "🔗",
            targetSidebarTab: 3),

        new ActionItem(
            id: "KvToQueryString",
            title: "Key-Values ➔ URL Query",
            category: "Code & Developer",
            description: "Encode key=value lines into URL query string (?k=v&...)",
            keywords: ["kv to query string", "url encode query", "params to url"],
            icon: "🔗",
            targetSidebarTab: 3),

        new ActionItem(
            id: "KvToJson",
            title: "Key-Values ➔ JSON Object",
            category: "Code & Developer",
            description: "Convert key=value lines into JSON dictionary object",
            keywords: ["kv to json", "key value to json", "properties to json"],
            icon: "📦",
            targetSidebarTab: 3),

        new ActionItem(
            id: "KvToYaml",
            title: "Key-Values ➔ YAML Map",
            category: "Code & Developer",
            description: "Convert key=value lines into YAML mapping",
            keywords: ["kv to yaml", "key value to yaml"],
            icon: "📄",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ExtractEmails",
            title: "Extract Email Addresses",
            category: "Code & Developer",
            description: "Find and extract all email addresses from text",
            keywords: ["emails", "extract email", "mail", "find emails", "addresses"],
            icon: "✉️",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ExtractUrls",
            title: "Extract URLs & Web Links",
            category: "Code & Developer",
            description: "Find and extract all web URLs and links from text",
            keywords: ["urls", "links", "extract urls", "http", "https", "web"],
            icon: "🌐",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ExtractIps",
            title: "Extract IPv4 Addresses",
            category: "Code & Developer",
            description: "Find and extract all IPv4 IP addresses from text",
            keywords: ["ip", "ipv4", "extract ip", "network", "ip addresses"],
            icon: "🌐",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ExtractGuids",
            title: "Extract GUIDs / UUIDs",
            category: "Code & Developer",
            description: "Find and extract all GUIDs / UUIDs from text",
            keywords: ["guid", "uuid", "extract guid", "unique identifier"],
            icon: "🆔",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ExtractNumbers",
            title: "Extract Numbers",
            category: "Code & Developer",
            description: "Find and extract all integer and decimal numbers from text",
            keywords: ["numbers", "extract numbers", "digits", "integers", "decimals"],
            icon: "🔢",
            targetSidebarTab: 3),

        // -------------------------------------------------------------
        // Case Conversion (Tab 4)
        // -------------------------------------------------------------
        new ActionItem(
            id: "CamelCase",
            title: "Convert to camelCase",
            category: "Case Conversion",
            description: "Convert words/lines to camelCase (e.g. myVariableName)",
            keywords: ["camelcase", "camel", "casing", "variable name"],
            icon: "🔡",
            targetSidebarTab: 4),

        new ActionItem(
            id: "PascalCase",
            title: "Convert to PascalCase",
            category: "Case Conversion",
            description: "Convert words/lines to PascalCase (e.g. MyClassName)",
            keywords: ["pascalcase", "pascal", "title case", "type name"],
            icon: "🔠",
            targetSidebarTab: 4),

        new ActionItem(
            id: "SnakeCase",
            title: "Convert to snake_case",
            category: "Case Conversion",
            description: "Convert words/lines to snake_case (e.g. my_variable_name)",
            keywords: ["snakecase", "snake", "underscore"],
            icon: "🐍",
            targetSidebarTab: 4),

        new ActionItem(
            id: "KebabCase",
            title: "Convert to kebab-case",
            category: "Case Conversion",
            description: "Convert words/lines to kebab-case (e.g. my-css-class)",
            keywords: ["kebabcase", "kebab", "dash", "hyphen"],
            icon: "🍢",
            targetSidebarTab: 4),

        new ActionItem(
            id: "ConstantCase",
            title: "Convert to CONSTANT_CASE",
            category: "Case Conversion",
            description: "Convert words/lines to CONSTANT_CASE (e.g. MAX_BUFFER_SIZE)",
            keywords: ["constantcase", "screaming snake", "uppercase snake", "constants"],
            icon: "📢",
            targetSidebarTab: 4),

        new ActionItem(
            id: "TitleCase",
            title: "Convert to Title Case",
            category: "Case Conversion",
            description: "Capitalize the first letter of each word",
            keywords: ["titlecase", "title case", "capitalize words", "heading"],
            icon: "📝",
            targetSidebarTab: 4),

        new ActionItem(
            id: "UpperCase",
            title: "Convert to UPPERCASE",
            category: "Case Conversion",
            description: "Convert all characters to uppercase",
            keywords: ["uppercase", "all caps", "upper", "caps"],
            icon: "🔠",
            targetSidebarTab: 4),

        new ActionItem(
            id: "LowerCase",
            title: "Convert to lowercase",
            category: "Case Conversion",
            description: "Convert all characters to lowercase",
            keywords: ["lowercase", "all lower", "lower", "small letters"],
            icon: "🔡",
            targetSidebarTab: 4),

        // -------------------------------------------------------------
        // Encodings & Formatting (Tab 4)
        // -------------------------------------------------------------
        new ActionItem(
            id: "UrlEncode",
            title: "URL Encode",
            category: "Encodings & Formatting",
            description: "Encode special characters for URLs (%20, etc.)",
            keywords: ["url encode", "percent encode", "uri encode", "escape url"],
            icon: "🔒",
            targetSidebarTab: 4),

        new ActionItem(
            id: "UrlDecode",
            title: "URL Decode",
            category: "Encodings & Formatting",
            description: "Decode percent-encoded URL text",
            keywords: ["url decode", "percent decode", "uri decode", "unescape url"],
            icon: "🔓",
            targetSidebarTab: 4),

        new ActionItem(
            id: "Base64Encode",
            title: "Base64 Encode",
            category: "Encodings & Formatting",
            description: "Encode text into Base64 format",
            keywords: ["base64", "b64", "base64 encode", "b64 encode", "binary to base64"],
            icon: "🔒",
            targetSidebarTab: 4),

        new ActionItem(
            id: "Base64Decode",
            title: "Base64 Decode",
            category: "Encodings & Formatting",
            description: "Decode Base64 string to plain text (with auto-padding)",
            keywords: ["base64", "b64", "base64 decode", "b64 decode", "decode base64"],
            icon: "🔓",
            targetSidebarTab: 4),

        new ActionItem(
            id: "HtmlEncode",
            title: "HTML Entity Encode",
            category: "Encodings & Formatting",
            description: "Encode HTML special characters (&lt;, &gt;, &amp;, etc.)",
            keywords: ["html encode", "html entities", "escape html"],
            icon: "🌐",
            targetSidebarTab: 4),

        new ActionItem(
            id: "HtmlDecode",
            title: "HTML Entity Decode",
            category: "Encodings & Formatting",
            description: "Decode HTML entities into characters",
            keywords: ["html decode", "unescape html", "decode html"],
            icon: "🌐",
            targetSidebarTab: 4),

        new ActionItem(
            id: "EscapeCSharp",
            title: "Escape C# String Literal",
            category: "Encodings & Formatting",
            description: "Escape special characters (\\n, \\r, \\t, \\\") for C# string literals",
            keywords: ["escape c#", "escape csharp", "escape string", "c# literal"],
            icon: "💻",
            targetSidebarTab: 4),

        new ActionItem(
            id: "UnescapeCSharp",
            title: "Unescape C# String Literal",
            category: "Encodings & Formatting",
            description: "Unescape C# string literal backslashes",
            keywords: ["unescape c#", "unescape csharp", "unescape string"],
            icon: "💻",
            targetSidebarTab: 4),

        new ActionItem(
            id: "JwtDecode",
            title: "Decode JWT Token",
            category: "Encodings & Formatting",
            description: "Inspect JSON Web Token Header, Payload, and Signature",
            keywords: ["jwt", "jwt decode", "json web token", "bearer token", "oauth", "token"],
            icon: "🎟️",
            targetSidebarTab: 4),

        new ActionItem(
            id: "Beautify",
            title: "Auto Beautify / Format",
            category: "Encodings & Formatting",
            description: "Auto-detect and format JSON, XML, HTML, or YAML",
            keywords: ["beautify", "format", "prettify", "auto format", "indent"],
            icon: "✨",
            targetSidebarTab: 4),

        // -------------------------------------------------------------
        // Navigation, Timeline & Productivity Tools
        // -------------------------------------------------------------
        new ActionItem(
            id: "SendOutputToInput",
            title: "Send Output ➔ Input",
            category: "Navigation & Workflow",
            description: "Copy current transformed output back to the input pane",
            keywords: ["send to input", "output to input", "chain", "pipe", "swap", "next transform"],
            icon: "🔁",
            shortcut: "Ctrl+Shift+I"),

        new ActionItem(
            id: "LoadFile",
            title: "Open File...",
            category: "Navigation & Workflow",
            description: "Load text, data, or structured file into input",
            keywords: ["open", "load file", "import", "open file", "browse"],
            icon: "📂",
            shortcut: "Ctrl+O"),

        new ActionItem(
            id: "ClearInput",
            title: "Clear Input",
            category: "Navigation & Workflow",
            description: "Clear all text in the input editor",
            keywords: ["clear", "reset", "erase", "empty input"],
            icon: "🗑️"),

        new ActionItem(
            id: "CreateSnapshot",
            title: "Create Timeline Snapshot",
            category: "Navigation & Workflow",
            description: "Save current input snapshot to history timeline",
            keywords: ["snapshot", "bookmark", "save snapshot", "history save"],
            icon: "📷"),

        new ActionItem(
            id: "HistoryBack",
            title: "History Back",
            category: "Navigation & Workflow",
            description: "Restore previous input snapshot from timeline",
            keywords: ["back", "undo", "history back", "previous"],
            icon: "⬅️",
            shortcut: "Alt+Left"),

        new ActionItem(
            id: "HistoryForward",
            title: "History Forward",
            category: "Navigation & Workflow",
            description: "Restore next input snapshot from timeline",
            keywords: ["forward", "redo", "history forward", "next"],
            icon: "➡️",
            shortcut: "Alt+Right"),

        new ActionItem(
            id: "ToggleRealTime",
            title: "Toggle Real-time Transform",
            category: "Navigation & Workflow",
            description: "Toggle live on-the-fly transformation while typing",
            keywords: ["real time", "realtime", "live transform", "toggle real time"],
            icon: "⚡"),

        new ActionItem(
            id: "ToggleWatchClipboard",
            title: "Toggle Watch Clipboard",
            category: "Navigation & Workflow",
            description: "Toggle automatic clipboard monitoring",
            keywords: ["clipboard", "watch clipboard", "auto paste", "monitor clipboard"],
            icon: "📋"),

        new ActionItem(
            id: "ToggleAutoSend",
            title: "Toggle Auto Output ➔ Input",
            category: "Navigation & Workflow",
            description: "Toggle automatic output chaining to input",
            keywords: ["auto send", "auto output to input", "pipeline mode"],
            icon: "🔁"),

        new ActionItem(
            id: "ToggleWordWrap",
            title: "Toggle Word Wrap",
            category: "Navigation & Workflow",
            description: "Toggle text wrapping across editors",
            keywords: ["word wrap", "wrap text", "toggle wrap"],
            icon: "↩"),

        new ActionItem(
            id: "ShowLinesTab",
            title: "Go to Lines Tab",
            category: "Navigation & Workflow",
            description: "Open Lines transformation tab in sidebar",
            keywords: ["lines tab", "tab lines", "switch to lines"],
            icon: "📐",
            targetSidebarTab: 0),

        new ActionItem(
            id: "ShowTabularTab",
            title: "Go to Tabular Tab",
            category: "Navigation & Workflow",
            description: "Open Tabular transformation tab in sidebar",
            keywords: ["tabular tab", "table tab", "switch to table"],
            icon: "📊",
            targetSidebarTab: 1),

        new ActionItem(
            id: "ShowStructuredTab",
            title: "Go to Structured Tab",
            category: "Navigation & Workflow",
            description: "Open Structured (JSON/YAML/XML) tab in sidebar",
            keywords: ["structured tab", "json tab", "xml tab", "tree tab", "switch to structured"],
            icon: "🌲",
            targetSidebarTab: 2),

        new ActionItem(
            id: "ShowCodeTab",
            title: "Go to Code & Dev Tab",
            category: "Navigation & Workflow",
            description: "Open Code & Developer tools tab in sidebar",
            keywords: ["code tab", "developer tab", "sql tab", "switch to code"],
            icon: "💻",
            targetSidebarTab: 3),

        new ActionItem(
            id: "ShowCaseEncTab",
            title: "Go to Case & Encodings Tab",
            category: "Navigation & Workflow",
            description: "Open Case & Encodings tab in sidebar",
            keywords: ["case tab", "encodings tab", "base64 tab", "switch to encodings"],
            icon: "🔠",
            targetSidebarTab: 4),

        new ActionItem(
            id: "ShowHistoryTab",
            title: "Go to History Timeline Tab",
            category: "Navigation & Workflow",
            description: "Open History & Timeline tab in sidebar",
            keywords: ["history tab", "timeline tab", "snapshots tab", "switch to history"],
            icon: "🕒",
            targetSidebarTab: 5),
    };

    public static IReadOnlyList<ActionItem> AllActions => _allActions;

    public static IReadOnlyList<ActionItem> Search(string? query)
    {
        return FuzzyMatcher.MatchActions(_allActions, query)
            .Select(r => r.Item)
            .ToList();
    }
}
