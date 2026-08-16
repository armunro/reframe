# TextForge

**TextForge** is a fast, modern desktop developer utility and text manipulation workbench built with **.NET 9** and **WPF**. It provides a comprehensive suite of real-time text transformation, tabular data conversion, code generation, string escaping/decoding, and text analysis tools in a responsive dark-themed interface.

---

## Table of Contents

- [Features](#features)
  - [1. Line Operations](#1-line-operations)
  - [2. Tabular Data & Table Conversions](#2-tabular-data--table-conversions)
  - [3. Developer Tools & Code Generation](#3-developer-tools--code-generation)
  - [4. Case Conversions](#4-case-conversions)
  - [5. Encodings, Escaping & Formatting](#5-encodings-escaping--formatting)
  - [6. Real-Time Text Analysis](#6-real-time-text-analysis)
  - [7. Input History & Timeline](#7-input-history--timeline)
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
  - **JSON Array of Objects**
  - **SQL INSERT Statements**
- **Column Operations**:
  - Extract specific columns with custom prefixes, suffixes, or delimiters.
  - Transform single columns (find/replace, prefix/suffix).
  - Filter tabular rows by column value.
  - Sort tabular data by column with natural numeric support.
  - Generate Key-Value maps from selected key and value columns (JSON or delimiter format).
- **Interactive Preview**: Live tabular data grid preview showing detected headers and structured rows.

### 3. Developer Tools & Code Generation
- **SQL `IN (...)` Clause**: Convert lists of numbers, strings, or IDs into formatted single-line or multi-line SQL `IN ('a', 'b', 'c')` clauses with SQL string escaping.
- **Code Array / Collection Generators**:
  - **C#**: `string[]`, `int[]`, `double[]`, `List<T>`
  - **TypeScript / JavaScript**: `const items = [...]`
  - **Python**: `items = [...]`
  - **JSON**: `["item1", "item2"]` or numeric array
- **URL Query String & Key-Value Tools**:
  - Convert URL Query Strings (`?key=value&foo=bar`) to Key-Value pairs or JSON objects.
  - Convert Key-Value pairs to URL Query Strings or JSON.
- **Data Extractors**: One-click extraction of:
  - Email addresses
  - URLs and links
  - IPv4 addresses
  - Numbers and hex values
  - GUIDs / UUIDs

### 4. Case Conversions
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

### 5. Encodings, Escaping & Formatting
- **URL Encode / Decode**: Standard percent-encoding and decoding.
- **HTML Encode / Decode**: HTML entity encoding and decoding.
- **Base64 Encode / Decode**: Base64 encoding with missing padding auto-correction.
- **JWT Decoder**: Inspect JSON Web Tokens with decoded, formatted Header and Payload JSON alongside token signatures.
- **C# String Escaper**: Escape or unescape special characters (`\r`, `\n`, `\t`, `\"`, `\\`) for use in C# string literals.
- **Beautifier / Formatter**: Indent and beautify JSON, XML, XHTML, and HTML.

### 6. Real-Time Text Analysis
TextForge dynamically analyzes the input text and reports:
- Detected format (CSV, TSV, Markdown Table, HTML Table, JSON, SQL IN clause, Key-Value pairs, Multi-line List, Numbers, etc.)
- Character count (total and excluding whitespace)
- Line count and non-empty line count
- Word count
- Distinct lines & duplicate count
- Detected delimiter, column count, and row count for tabular formats

### 7. Input History & Timeline
- Keeps a chronological history of inputs and applied operations.
- Search and filter history entries.
- One-click restore of previous inputs and configurations.

---

## Architecture & Solution Structure

```
textforge/
├── TextForge/               # Main WPF Application
│   ├── Controls/            # Custom controls (BindableTextEditor with AvalonEdit)
│   ├── Converters/          # WPF Value Converters
│   ├── Highlighting/        # AvalonEdit syntax highlighting definitions (dark theme)
│   ├── ViewModels/          # MainViewModel & RelayCommand MVVM implementation
│   ├── MainWindow.xaml      # Main user interface layout and styling
│   └── App.xaml             # Application startup and resource definitions
│
├── TextForge.Core/          # Core Business Logic & Transformers (.NET 9)
│   ├── Analysis/            # Text analyzer and statistics engine
│   ├── History/             # Input and operation history management
│   ├── State/               # UI section state persistence
│   ├── Tabular/             # CSV, TSV, Markdown, and HTML table parsers & converters
│   └── Transformers/        # Line, Case, Developer, Encoding, and Beautifier transformers
│
├── Catalyst.WPF/            # Reusable WPF UI Components & Theme Helpers
│   └── Themes/              # Theme styles and control templates
│
└── TextForge.Tests/         # Comprehensive Unit Tests (xUnit)
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
- **Editor Control**: [AvalonEdit](https://github.com/icsharpcode/AvalonEdit) for syntax-highlighted code and text editing
- **Testing**: [xUnit](https://xunit.net/) & `Microsoft.NET.Test.Sdk`
- **Pattern**: MVVM (Model-View-ViewModel) with XAML data binding

---

## License

This project is licensed under the terms specified in the [LICENSE](LICENSE) file.