# TextForge

**TextForge** is a fast, modern desktop developer utility and text manipulation workbench built with **.NET 9** and **WPF**. It provides a comprehensive suite of real-time text transformation, tabular data conversion, code generation, string escaping/decoding, and text analysis tools in a responsive dark-themed interface.

---

## Table of Contents

- [Features](#features)
  - [1. Line Operations](#1-line-operations)
  - [2. Tabular Data & Table Conversions](#2-tabular-data--table-conversions)
  - [3. Structured Data, Conversions & Tree Viewer](#3-structured-data-conversions--tree-viewer)
  - [4. Developer Tools & Code Generation](#4-developer-tools--code-generation)
  - [5. Case Conversions](#5-case-conversions)
  - [6. Encodings, Escaping & Formatting](#6-encodings-escaping--formatting)
  - [7. Real-Time Text Analysis](#7-real-time-text-analysis)
  - [8. Input History & Timeline](#8-input-history--timeline)
  - [9. File Loading & Drag-and-Drop](#9-file-loading--drag-and-drop)
  - [10. Productivity Toggles & Transformation Chaining](#10-productivity-toggles--transformation-chaining)
- [Architecture & Solution Structure](#architecture--solution-structure)
- [Prerequisites](#prerequisites)
- [Getting Started](#getting-started)
  - [Build](#build)
  - [Run](#run)
  - [Run Tests](#run-tests)
- [Tech Stack & Libraries](#tech-stack--libraries)
- [License](#license)

---

## Features

### 1. Line Operations
- **Quoting & Wrapping**: Wrap lines with single quotes (`'`), double quotes (`"`), backticks (`` ` ``), parentheses `()`, brackets `[]`, braces `{}`, or custom prefix/suffix with automatic inner-quote escaping.
- **Join Lines**: Join multiple lines into a single line with custom delimiters, optional item quotes, and enclosing prefix/suffix.
- **Split Lines**: Split text by delimiters (comma, semicolon, tab, pipe, whitespace) or custom regular expressions into multiple lines.
- **Sort & Deduplication**: Sort lines (alphabetical, case-insensitive, natural numeric, length, reverse) and deduplicate (distinct items, duplicates only, or count occurrences).
- **Trimming & Cleaning**: Trim start/end whitespace, collapse multiple spaces, and strip empty lines.
- **Prefix / Suffix & Numbering**: Add prefixes or suffixes (with options to skip first/last line), or apply custom line numbering (`1. `, `[1]`, `#1: `).
- **Line Filtering & Search-Replace**: Filter lines matching or excluding text/regex, and perform regex or case-sensitive find & replace across all lines.
- **Regex Extraction**: Extract matching patterns or specific capture groups across all lines.

### 2. Tabular Data & Table Conversions
- **Format Interoperability**: Parse and convert seamlessly between:
  - **CSV** (Comma-Separated Values)
  - **TSV** (Tab-Separated Values)
  - **Markdown Tables** (formatted with aligned pipes and headers)
  - **HTML Tables** (`<table>`, `<tr>`, `<th>`, `<td>`)
  - **JSON Array of Objects** & JSON Array of Arrays
  - **YAML Array of Objects** & YAML Sequences
  - **SQL INSERT Statements**
- **Surrogate Headers & Header Overrides**:
  - Add custom surrogate headers for tables without existing headers (or when "First row contains headers" is unchecked).
  - Override existing table headers with custom column names (delimited by comma, tab, pipe, semicolon, or newlines).
  - One-click auto-generation of sequential surrogate headers (`Col1, Col2...` or `Column_1, Column_2...`).
  - Prepend or replace header rows directly in tabular output or table grid preview.
  - Automatically reflected across all tabular conversions (JSON objects, YAML, SQL INSERT statements, Markdown, etc.).
- **Column Operations**:
  - Extract specific columns with custom prefixes, suffixes, or delimiters.
  - Transform single columns (find/replace, prefix/suffix).
  - Filter tabular rows by column value.
  - Sort tabular data by column with natural numeric support.
  - Generate Key-Value maps from selected key and value columns (JSON, YAML, or URL query format).
  - **Rest of Properties as Value Object**: Option to include all remaining columns/properties in the generated value object (e.g. dictionary/JSON object/YAML map of all other columns keyed by the chosen primary key column).
- **Interactive Preview**: Live tabular data grid preview showing detected headers and structured rows with real-time surrogate header editing.

### 3. Structured Data, Conversions & Tree Viewer
- **Dedicated Structured Transformations Sidebar Tab**:
  - Automatically highlights when JSON, YAML, or XML documents are detected.
  - Organized expanders for format conversions, key casing, key/field filtering, querying/extraction, deep sorting, flattening, minification, and code/schema generation.
- **Interactive Structured Tree Viewer**:
  - Automatically parses JSON, YAML, and XML documents into an interactive, syntax-highlighted hierarchical Tree View.
  - Visual type badges for Objects (`{ }`), Arrays (`[ ]`), XML Elements (`< >`), Attributes (`@`), Strings (`str`), Numbers (`num`), Booleans (`bool`), and Nulls (`null`).
  - Real-time instant search filter across node keys, values, and paths.
  - One-click Expand All, Collapse All, and quick copy of JSONPath/XPath, node values, or node subtrees.
- **Format Interoperability**:
  - **XML ➔ JSON** & **JSON ➔ XML** (preserves attributes with `@attr` notation and handles child arrays).
  - **XML ➔ YAML** & **YAML ➔ XML**.
  - **JSON ➔ YAML** & **YAML ➔ JSON** with type preservation.
  - **Structured ➔ Tabular**: Convert JSON/YAML/XML datasets directly into CSV, TSV, or Markdown tables.
- **Key Casing & Renaming**:
  - Recursively transform all object keys/property names across JSON, YAML, or XML to `camelCase`, `PascalCase`, `snake_case`, `kebab-case`, or `CONSTANT_CASE`.
- **Key & Field Filtering**:
  - **Pick / Keep Keys**: Keep only specified keys across nested objects.
  - **Omit / Remove Keys**: Strip specified sensitive or unnecessary keys.
  - **Remove Nulls & Empty**: Recursively remove `null` values, empty strings, and empty objects/arrays.
- **Querying & Value Extraction**:
  - **XPath Query & Wildcards**: Execute full XPath 1.0 expressions across XML, JSON, and YAML structured data. Supports element wildcards (`//*`, `//book/*`, `/*/*/*`), attribute wildcards (`//@*`, `//book/@*`, `//@id`), element filters/predicates (`//book[price > 40]`, `//book[1]`), text selectors (`//title/text()`), and XPath functions (`count(//book)`, `sum(//price)`).
  - **Namespace-Tolerant XPath**: Transparently evaluates queries across XML documents with default or unprefixed namespaces.
  - **Extract XPath Values**: Extract matching inner text or scalar values into a clean line-delimited list.
  - **Extract XPath Attributes**: Extract all matching attributes and values formatted as `@attr="value"`.
  - **JSONPath & Node Query**: Query structured documents with JSONPath expressions or property names (e.g., `$.users[*].name`, `firstName`).
  - **Extract All Paths**: Extract all distinct JSONPaths or XPaths across the structure.
  - **Extract All Keys**: Extract unique property / element names.
  - **Extract All Values**: Extract all scalar values into a line list.
- **Flattening & Unflattening**:
  - **Flatten to Paths**: Flatten nested structures into dot-notation / bracket-index key-value paths (`user.address.city = "Seattle"`).
  - **Flatten to Flat JSON**: Flatten nested structures into a flat JSON object with dot-notation keys (`{"user.address.city": "Seattle"}`).
  - **Unflatten to Structure**: Reconstruct nested JSON / YAML objects from dot-notation or path key-value lines.
- **Deep Structure Operations**:
  - **Deep Key Sorting**: Recursively sort object / mapping keys alphabetically ascending (A-Z) or descending (Z-A).
  - **Minify JSON & XML**: Strip whitespace and newlines for compact storage / payload transfer.
  - **Beautify & Format**: Multi-format beautification and indentation for JSON, YAML, and XML.
- **Code & Schema Generation**:
  - **TypeScript Interfaces**: Infer and generate typed TypeScript interfaces (`export interface ...`) from JSON/YAML.
  - **C# POCO Classes**: Infer and generate typed C# classes with properties (`public class ...`).
  - **JSON Schema**: Generate valid draft-07 JSON Schemas with typed properties and required field lists.

### 4. Developer Tools & Code Generation
- **SQL `IN (...)` Clause**: Convert lists of numbers, strings, or IDs into formatted single-line or multi-line SQL `IN ('a', 'b', 'c')` clauses with SQL string escaping.
- **Code Array / Collection Generators**:
  - **C#**: `string[]`, `int[]`, `double[]`, `List<T>`
  - **TypeScript / JavaScript**: `const items = [...]`
  - **Python**: `items = [...]`
  - **JSON**: `["item1", "item2"]` or numeric array
  - **YAML**: `- item1\n- item2` or numeric list
- **URL Query String, JSON & YAML Key-Value Tools**:
  - Convert URL Query Strings (`?key=value&foo=bar`) to Key-Value pairs, JSON objects, or YAML maps.
  - Convert Key-Value pairs to URL Query Strings, JSON, or YAML.
  - Convert between JSON and YAML with type-preserving roundtrips.
- **Data Extractors**: One-click extraction of:
  - Email addresses
  - URLs and links
  - IPv4 addresses
  - Numbers and hex values
  - GUIDs / UUIDs

### 5. Case Conversions
Easily convert text per-line or across entire blocks into:
- `camelCase`
- `PascalCase`
- `snake_case`
- `kebab-case`
- `CONSTANT_CASE` (Screaming Snake Case)
- `Title Case`
- `UPPERCASE`
- `lowercase`
- `dot.case`
- `path/case`

### 6. Encodings, Escaping & Formatting
- **URL Encode / Decode**: Standard percent-encoding and decoding.
- **HTML Encode / Decode**: HTML entity encoding and decoding.
- **Base64 Encode / Decode**: Base64 encoding with missing padding auto-correction.
- **JWT Decoder**: Inspect JSON Web Tokens with decoded, formatted Header and Payload JSON alongside token signatures.
- **C# String Escaper**: Escape or unescape special characters (`\r`, `\n`, `\t`, `\"`, `\\`) for use in C# string literals.
- **Beautifier / Formatter**: Indent and beautify JSON, XML, XHTML, HTML, and YAML.
- **JSON ➔ YAML & YAML ➔ JSON**: Direct bidirectional conversion between JSON and YAML.

### 7. Real-Time Text Analysis
TextForge dynamically analyzes the input text and reports:
- Detected format (CSV, TSV, Markdown Table, HTML Table, JSON, YAML, SQL IN clause, Key-Value pairs, Multi-line List, Numbers, etc.)
- Character count (total and excluding whitespace)
- Line count and non-empty line count
- Word count
- Distinct lines & duplicate count
- Detected delimiter, column count, and row count for tabular formats

### 8. Input History & Timeline
- Keeps a chronological history of inputs and applied operations.
- Search and filter history entries.
- One-click restore of previous inputs and configurations.

### 9. File Loading & Drag-and-Drop
- **File Dialog**: Open text, tabular data, JSON, XML, Markdown, HTML, SQL, and log files using `📂 Open` button or `Ctrl+O`.
- **Drag-and-Drop**: Drag files directly from Windows File Explorer onto the TextForge window or editor pane to load them instantly.
- **Auto-Formatting & History**: Automatically formats structured inputs upon load and records the loaded file in the history timeline.

### 10. Productivity Toggles & Transformation Chaining
- **⚡ Real-time Transform**: Computes outputs on the fly as you type or change transformation parameters.
- **🔁 Auto Output ➔ Input**: When enabled in the status bar, automatically sends the transformed output back to the input pane after every operation, enabling seamless sequential transformation pipelines.
- **↩ Word Wrap**: Toggles word wrap across input and output editors.

---

## Architecture & Solution Structure

```
textforge/
├── TextForge/               # Main WPF Application
│   ├── Controls/            # Custom controls (BindableTextEditor with AvalonEdit)
│   ├── Converters/          # WPF Value Converters
│   ├── Highlighting/        # AvalonEdit syntax highlighting definitions (dark theme)
│   ├── Themes/              # Theme styles and control templates (SharedTheme.xaml)
│   ├── ViewModels/          # MainViewModel & RelayCommand MVVM implementation
│   ├── MainWindow.xaml      # Main user interface layout and styling
│   └── App.xaml             # Application startup and resource definitions
│
├── TextForge.Core/          # Core Business Logic & Transformers (.NET 9)
│   ├── Analysis/            # Text analyzer, models & format detection
│   │   ├── Analyzers/       # ITextAnalyzer & DefaultTextAnalyzer implementations
│   │   └── Models/          # TextAnalysisResult & analysis models
│   ├── History/             # Input and operation history management
│   ├── State/               # UI section state persistence
│   ├── Structured/          # JSON, XML, YAML structured data parser & transformers
│   │   ├── Models/          # StructuredDataNode & StructuredNodeType
│   │   ├── Parsers/         # IStructuredDataParser & StructuredDataParserService
│   │   └── Transformers/    # IStructuredTransformer & StructuredTransformerService
│   ├── Tabular/             # CSV, TSV, Markdown, HTML, and JSON/YAML table processing
│   │   ├── Converters/      # ITabularConverter & TabularConverterService
│   │   ├── Models/          # TabularData representation
│   │   └── Parsers/         # ITabularParser, TabularParserService & format-specific parsers
│   └── Transformers/        # Modular text transformation services and pipelines
│       ├── Case/            # Casing transformations (camel, Pascal, snake, kebab, etc.)
│       ├── Core/            # ITextTransformer, pipeline execution & transformer registry
│       ├── Developer/       # Code array generators, SQL IN, JSON/C# escape
│       ├── Encoding/        # Base64, URL, HTML, JWT encoders/decoders
│       ├── Formatting/      # JSON, XML, YAML beautifiers and formatters
│       └── Line/            # Quoting, sorting, deduplication, filtering, line numbering
│
└── TextForge.Tests/         # Comprehensive Unit Tests (xUnit)
    ├── FileLoadingTests.cs
    ├── HighlightingTests.cs
    ├── HtmlTableAndTabularTests.cs
    ├── InputHistoryTests.cs
    ├── SectionStateTests.cs
    ├── TabHighlightingTests.cs
    ├── TextBeautifierTests.cs
    └── TransformerTests.cs
```

---

## Prerequisites

- **OS**: Windows 10 / 11 (x64 / x86 / ARM64)
- **SDK**: [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) or higher
- **IDE** (Optional): JetBrains Rider 2024.3+, Visual Studio 2022 17.12+, or VS Code with C# Dev Kit

---

## Getting Started

### Build

Clone the repository and build the solution using the .NET CLI:

```powershell
dotnet build textforge.sln
```

### Run

Run the desktop application:

```powershell
dotnet run --project TextForge/TextForge.csproj
```

### Run Tests

Execute the unit test suite:

```powershell
dotnet test TextForge.Tests/TextForge.Tests.csproj
```

---

## Tech Stack & Libraries

- **Framework**: [.NET 9.0](https://dotnet.microsoft.com/) / WPF (Windows Presentation Foundation)
- **UI Library**: [WPF-UI](https://github.com/lepoco/wpfui) for modern Windows 11 Fluent design, Mica backdrop, custom TitleBar, and Fluent controls
- **Editor Control**: [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) for syntax-highlighted code and text editing
- **Testing**: [xUnit](https://xunit.net/) & `Microsoft.NET.Test.Sdk`
- **Pattern**: MVVM (Model-View-ViewModel) with XAML data binding

---

## License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.