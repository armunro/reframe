using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Reframe.Core.Tabular.Models;
using Reframe.Core.Transformers.Case;

namespace Reframe.Core.Tabular.Formulas;

/// <summary>
/// Description of a formula function available in the tabular formula engine.
/// </summary>
public record FormulaFunctionHelp(string Name, string Syntax, string Description, string Category, string Example);

/// <summary>
/// High-performance expression parser and evaluator for Excel-like tabular formulas,
/// with rich support for text manipulation, regex matching/extracting/replacing,
/// substring, splitting, joining, and column references.
/// </summary>
public static class TabularFormulaEngine
{
    private static readonly List<FormulaFunctionHelp> _availableFunctions = new()
    {
        // Text Combining & Splitting
        new("CONCAT", "CONCAT(text1, text2, ...)", "Concatenates two or more text values", "Text", "CONCAT([FirstName], \" \", [LastName])"),
        new("JOIN", "JOIN(delimiter, text1, text2, ...)", "Joins text arguments with a specified delimiter", "Text", "JOIN(\", \", [City], [State], [Zip])"),
        new("TEXTJOIN", "TEXTJOIN(delimiter, ignore_empty, text1, text2, ...)", "Joins text strings with a delimiter, optionally skipping empty values", "Text", "TEXTJOIN(\" - \", TRUE, [Code], [Dept], [Title])"),
        new("SPLIT", "SPLIT(text, delimiter, index_1based)", "Splits text by delimiter and returns the element at the 1-based index (or negative for index from end)", "Text", "SPLIT([FullName], \" \", 1)"),
        new("SPLIT_PART", "SPLIT_PART(text, delimiter, index_1based)", "Alias for SPLIT", "Text", "SPLIT_PART([Email], \"@\", 2)"),

        // Substrings & Slicing
        new("SUBSTRING", "SUBSTRING(text, start_1based, [length])", "Extracts a substring starting at 1-based index for optional length", "Text", "SUBSTRING([ID], 3, 4)"),
        new("MID", "MID(text, start_1based, length)", "Extracts characters from the middle of a text string", "Text", "MID([ProductCode], 2, 3)"),
        new("LEFT", "LEFT(text, [num_chars])", "Returns the leftmost characters of a text string", "Text", "LEFT([ZipCode], 5)"),
        new("RIGHT", "RIGHT(text, [num_chars])", "Returns the rightmost characters of a text string", "Text", "RIGHT([PhoneNumber], 4)"),

        // Search, Find, IndexOf
        new("FIND", "FIND(find_text, within_text, [start_1based])", "Case-sensitive search returning 1-based position (or 0 if not found)", "Search", "FIND(\"-\", [SKU])"),
        new("SEARCH", "SEARCH(find_text, within_text, [start_1based])", "Case-insensitive search returning 1-based position (or 0 if not found)", "Search", "SEARCH(\"reframe\", [Description])"),
        new("INDEXOF", "INDEXOF(within_text, find_text, [start_1based])", "Returns 1-based index of find_text in within_text (or 0 if not found)", "Search", "INDEXOF([Path], \"/\")"),
        new("CONTAINS", "CONTAINS(text, substring, [case_sensitive])", "Returns TRUE if text contains substring", "Search", "CONTAINS([Status], \"active\")"),
        new("STARTSWITH", "STARTSWITH(text, prefix, [case_sensitive])", "Returns TRUE if text starts with prefix", "Search", "STARTSWITH([URL], \"https://\")"),
        new("ENDSWITH", "ENDSWITH(text, suffix, [case_sensitive])", "Returns TRUE if text ends with suffix", "Search", "ENDSWITH([FileName], \".json\")"),

        // Replace & Substitute
        new("REPLACE", "REPLACE(text, old_text, new_text)", "Replaces all occurrences of old_text with new_text", "Text", "REPLACE([Phone], \"-\", \"\")"),
        new("SUBSTITUTE", "SUBSTITUTE(text, old_text, new_text, [instance_num])", "Replaces occurrences or specific instance of text in string", "Text", "SUBSTITUTE([Address], \"St\", \"Street\", 1)"),

        // Regex Operations
        new("REGEXMATCH", "REGEXMATCH(text, pattern, [case_sensitive])", "Returns TRUE if text matches the regular expression pattern", "Regex", "REGEXMATCH([Email], \"^[\\w.-]+@[\\w.-]+\\.[a-z]{2,}$\")"),
        new("REGEXEXTRACT", "REGEXEXTRACT(text, pattern, [group_index_or_name])", "Extracts regex match or capture group", "Regex", "REGEXEXTRACT([OrderString], \"#(\\d+)\", 1)"),
        new("REGEXREPLACE", "REGEXREPLACE(text, pattern, replacement)", "Replaces regex matches with replacement string", "Regex", "REGEXREPLACE([Text], \"\\s+\", \" \")"),

        // Casing & Formatting
        new("UPPER", "UPPER(text)", "Converts text to uppercase", "Formatting", "UPPER([CountryCode])"),
        new("LOWER", "LOWER(text)", "Converts text to lowercase", "Formatting", "LOWER([Email])"),
        new("PROPER", "PROPER(text)", "Converts text to Title Case", "Formatting", "PROPER([Name])"),
        new("TRIM", "TRIM(text)", "Removes leading and trailing whitespace", "Formatting", "TRIM([RawInput])"),
        new("LTRIM", "LTRIM(text)", "Removes leading whitespace", "Formatting", "LTRIM([Text])"),
        new("RTRIM", "RTRIM(text)", "Removes trailing whitespace", "Formatting", "RTRIM([Text])"),
        new("LEN", "LEN(text)", "Returns character length of text", "Formatting", "LEN([Password])"),
        new("PADLEFT", "PADLEFT(text, total_width, [padding_char])", "Pads text on the left to specified width", "Formatting", "PADLEFT([ID], 6, \"0\")"),
        new("PADRIGHT", "PADRIGHT(text, total_width, [padding_char])", "Pads text on the right to specified width", "Formatting", "PADRIGHT([Category], 15, \" \")"),
        new("REPEAT", "REPEAT(text, count)", "Repeats text a specified number of times", "Formatting", "REPEAT(\"*\", 5)"),
        new("REVERSE", "REVERSE(text)", "Reverses the characters in text", "Formatting", "REVERSE([Code])"),

        // Logic & Control Flow
        new("IF", "IF(condition, value_if_true, [value_if_false])", "Returns one value if condition is true and another if false", "Logic", "IF([Age] >= 18, \"Adult\", \"Minor\")"),
        new("IFS", "IFS(cond1, val1, cond2, val2, ...)", "Evaluates multiple conditions in order and returns corresponding value", "Logic", "IFS([Score] >= 90, \"A\", [Score] >= 80, \"B\", TRUE, \"C\")"),
        new("SWITCH", "SWITCH(expr, val1, res1, val2, res2, ..., [default])", "Evaluates expression against a list of values", "Logic", "SWITCH([Tier], 1, \"Gold\", 2, \"Silver\", \"Bronze\")"),
        new("IFERROR", "IFERROR(value, value_if_error)", "Returns value_if_error if calculation results in an error", "Logic", "IFERROR([A] / [B], \"N/A\")"),
        new("COALESCE", "COALESCE(val1, val2, ...)", "Returns the first non-empty value", "Logic", "COALESCE([NickName], [FirstName], \"Guest\")"),
        new("ISBLANK", "ISBLANK(value)", "Returns TRUE if value is null or empty", "Logic", "ISBLANK([MiddleName])"),
        new("NOT", "NOT(logical_value)", "Reverses the logical value", "Logic", "NOT(ISBLANK([Email]))"),
        new("AND", "AND(logical1, logical2, ...)", "Returns TRUE if all arguments are true", "Logic", "AND([Age] >= 21, [Active] = \"TRUE\")"),
        new("OR", "OR(logical1, logical2, ...)", "Returns TRUE if any argument is true", "Logic", "OR([Role] = \"Admin\", [Role] = \"Manager\")"),

        // Math & Utilities
        new("ROUND", "ROUND(number, [num_digits])", "Rounds a number to a specified number of digits", "Math", "ROUND([Price] * 1.08, 2)"),
        new("INT", "INT(number)", "Rounds a number down to the nearest integer", "Math", "INT([Quantity])"),
        new("ABS", "ABS(number)", "Returns the absolute value of a number", "Math", "ABS([Balance])"),
        new("SUM", "SUM(num1, num2, ...)", "Sums all numeric arguments", "Math", "SUM([Subtotal], [Tax], [Shipping])"),
        new("ROW", "ROW()", "Returns the 1-based row number", "Utility", "ROW()"),
        new("COL", "COL(col_name_or_1based_index)", "Returns the value of a column dynamically", "Utility", "COL(\"Price\")")
    };

    public static IReadOnlyList<FormulaFunctionHelp> GetAvailableFunctions() => _availableFunctions;

    /// <summary>
    /// Evaluates a formula for every row of the given table and returns a new TabularData with the added column.
    /// </summary>
    public static TabularData AddCalculatedColumn(TabularData table, string columnName, string formula, int? insertIndex = null)
    {
        var result = table.Clone();
        string cleanColName = string.IsNullOrWhiteSpace(columnName)
            ? $"Column_{result.Columns.Count + 1}"
            : columnName.Trim();

        var computedValues = EvaluateColumn(result, formula);

        int targetIndex = insertIndex.HasValue
            ? Math.Clamp(insertIndex.Value, 0, result.Columns.Count)
            : result.Columns.Count;

        result.Columns.Insert(targetIndex, cleanColName);

        for (int r = 0; r < result.Rows.Count; r++)
        {
            string val = r < computedValues.Count ? computedValues[r] : string.Empty;
            if (targetIndex < result.Rows[r].Count)
            {
                result.Rows[r].Insert(targetIndex, val);
            }
            else
            {
                while (result.Rows[r].Count < targetIndex)
                {
                    result.Rows[r].Add(string.Empty);
                }
                result.Rows[r].Add(val);
            }
        }

        return result;
    }

    /// <summary>
    /// Evaluates a formula across all rows in a table.
    /// </summary>
    public static List<string> EvaluateColumn(TabularData table, string formula)
    {
        var list = new List<string>(table.Rows.Count);
        if (string.IsNullOrWhiteSpace(formula))
        {
            for (int i = 0; i < table.Rows.Count; i++)
            {
                list.Add(string.Empty);
            }
            return list;
        }

        // Parse AST once for performance across all rows
        FormulaAstNode? ast = null;
        string? parseError = null;
        try
        {
            ast = ParseFormula(formula);
        }
        catch (Exception ex)
        {
            parseError = $"#ERROR: {ex.Message}";
        }

        for (int r = 0; r < table.Rows.Count; r++)
        {
            if (ast == null)
            {
                list.Add(parseError ?? "#ERROR!");
                continue;
            }

            try
            {
                var context = new FormulaEvaluationContext(table.Columns, table.Rows[r], r);
                object? evaluated = ast.Evaluate(context);
                list.Add(FormatResult(evaluated));
            }
            catch (Exception ex)
            {
                list.Add($"#ERROR: {ex.Message}");
            }
        }

        return list;
    }

    /// <summary>
    /// Evaluates a formula for a single row given column names and row values.
    /// </summary>
    public static string Evaluate(string formula, IReadOnlyList<string> columns, IReadOnlyList<string> rowValues, int rowIndex = 0)
    {
        if (string.IsNullOrWhiteSpace(formula))
            return string.Empty;

        try
        {
            var ast = ParseFormula(formula);
            var context = new FormulaEvaluationContext(columns, rowValues, rowIndex);
            object? res = ast.Evaluate(context);
            return FormatResult(res);
        }
        catch (Exception ex)
        {
            return $"#ERROR: {ex.Message}";
        }
    }

    private static string FormatResult(object? val)
    {
        if (val == null) return string.Empty;
        if (val is bool b) return b ? "TRUE" : "FALSE";
        if (val is double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return "#NUM!";
            double rounded = Math.Round(d, 10);
            return rounded % 1 == 0 ? ((long)rounded).ToString(CultureInfo.InvariantCulture) : rounded.ToString("G", CultureInfo.InvariantCulture);
        }
        if (val is int or long or short or byte)
        {
            return Convert.ToString(val, CultureInfo.InvariantCulture) ?? "";
        }
        return val.ToString() ?? string.Empty;
    }

    #region Parser & Lexer

    public static FormulaAstNode ParseFormula(string formula)
    {
        string trimmed = formula.Trim();
        if (trimmed.StartsWith('='))
        {
            trimmed = trimmed[1..].Trim();
        }

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new LiteralNode(string.Empty);
        }

        var tokens = Tokenize(trimmed);
        var parser = new FormulaParser(tokens);
        var node = parser.ParseExpression();
        if (!parser.IsAtEnd)
        {
            throw new InvalidOperationException($"Unexpected token '{parser.CurrentToken.Text}' at position {parser.CurrentToken.Position}");
        }
        return node;
    }

    internal enum TokenType
    {
        Number,
        String,
        Identifier,
        BracketIdentifier,
        Plus,
        Minus,
        Multiply,
        Divide,
        Modulo,
        Ampersand,
        Equals,
        DoubleEquals,
        NotEquals,
        LessThan,
        LessThanOrEqual,
        GreaterThan,
        GreaterThanOrEqual,
        OpenParen,
        CloseParen,
        Comma,
        EndOfFile
    }

    internal record Token(TokenType Type, string Text, int Position, object? LiteralValue = null);

    private static List<Token> Tokenize(string input)
    {
        var tokens = new List<Token>();
        int pos = 0;
        int len = input.Length;

        while (pos < len)
        {
            char c = input[pos];

            if (char.IsWhiteSpace(c))
            {
                pos++;
                continue;
            }

            // String literals: "..." or '...'
            if (c is '"' or '\'')
            {
                char quoteChar = c;
                int startPos = pos;
                pos++;
                var sb = new StringBuilder();
                while (pos < len)
                {
                    if (input[pos] == '\\' && pos + 1 < len)
                    {
                        char next = input[pos + 1];
                        if (next == 'n') { sb.Append('\n'); pos += 2; }
                        else if (next == 'r') { sb.Append('\r'); pos += 2; }
                        else if (next == 't') { sb.Append('\t'); pos += 2; }
                        else if (next == quoteChar) { sb.Append(quoteChar); pos += 2; }
                        else if (next == '\\') { sb.Append('\\'); pos += 2; }
                        else
                        {
                            sb.Append('\\');
                            sb.Append(next);
                            pos += 2;
                        }
                    }
                    else if (input[pos] == quoteChar)
                    {
                        // Check for escaped quote by doubling e.g. "" or ''
                        if (pos + 1 < len && input[pos + 1] == quoteChar)
                        {
                            sb.Append(quoteChar);
                            pos += 2;
                        }
                        else
                        {
                            pos++;
                            break;
                        }
                    }
                    else
                    {
                        sb.Append(input[pos]);
                        pos++;
                    }
                }
                tokens.Add(new Token(TokenType.String, sb.ToString(), startPos, sb.ToString()));
                continue;
            }

            // Bracketed Column Name: [Column Name]
            if (c == '[')
            {
                int startPos = pos;
                pos++;
                var sb = new StringBuilder();
                while (pos < len && input[pos] != ']')
                {
                    sb.Append(input[pos]);
                    pos++;
                }
                if (pos < len && input[pos] == ']')
                {
                    pos++;
                }
                tokens.Add(new Token(TokenType.BracketIdentifier, sb.ToString(), startPos, sb.ToString()));
                continue;
            }

            // Numbers
            if (char.IsDigit(c) || (c == '.' && pos + 1 < len && char.IsDigit(input[pos + 1])))
            {
                int startPos = pos;
                while (pos < len && (char.IsDigit(input[pos]) || input[pos] == '.'))
                {
                    pos++;
                }
                string numStr = input[startPos..pos];
                if (double.TryParse(numStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double numVal))
                {
                    tokens.Add(new Token(TokenType.Number, numStr, startPos, numVal));
                }
                else
                {
                    tokens.Add(new Token(TokenType.String, numStr, startPos, numStr));
                }
                continue;
            }

            // Two-character operators
            if (pos + 1 < len)
            {
                string twoChar = input.Substring(pos, 2);
                if (twoChar == "==")
                {
                    tokens.Add(new Token(TokenType.DoubleEquals, "==", pos));
                    pos += 2;
                    continue;
                }
                if (twoChar is "!=" or "<>")
                {
                    tokens.Add(new Token(TokenType.NotEquals, twoChar, pos));
                    pos += 2;
                    continue;
                }
                if (twoChar == "<=")
                {
                    tokens.Add(new Token(TokenType.LessThanOrEqual, "<=", pos));
                    pos += 2;
                    continue;
                }
                if (twoChar == ">=")
                {
                    tokens.Add(new Token(TokenType.GreaterThanOrEqual, ">=", pos));
                    pos += 2;
                    continue;
                }
            }

            // Single character operators
            switch (c)
            {
                case '+':
                    tokens.Add(new Token(TokenType.Plus, "+", pos++));
                    continue;
                case '-':
                    tokens.Add(new Token(TokenType.Minus, "-", pos++));
                    continue;
                case '*':
                    tokens.Add(new Token(TokenType.Multiply, "*", pos++));
                    continue;
                case '/':
                    tokens.Add(new Token(TokenType.Divide, "/", pos++));
                    continue;
                case '%':
                    tokens.Add(new Token(TokenType.Modulo, "%", pos++));
                    continue;
                case '&':
                    tokens.Add(new Token(TokenType.Ampersand, "&", pos++));
                    continue;
                case '=':
                    tokens.Add(new Token(TokenType.Equals, "=", pos++));
                    continue;
                case '<':
                    tokens.Add(new Token(TokenType.LessThan, "<", pos++));
                    continue;
                case '>':
                    tokens.Add(new Token(TokenType.GreaterThan, ">", pos++));
                    continue;
                case '(':
                    tokens.Add(new Token(TokenType.OpenParen, "(", pos++));
                    continue;
                case ')':
                    tokens.Add(new Token(TokenType.CloseParen, ")", pos++));
                    continue;
                case ',':
                case ';':
                    tokens.Add(new Token(TokenType.Comma, ",", pos++));
                    continue;
            }

            // Identifiers / words / function names / column references ($1, A, Col1, etc.)
            if (char.IsLetter(c) || c is '_' or '$')
            {
                int startPos = pos;
                while (pos < len && (char.IsLetterOrDigit(input[pos]) || input[pos] is '_' or '$'))
                {
                    pos++;
                }
                string ident = input[startPos..pos];
                tokens.Add(new Token(TokenType.Identifier, ident, startPos, ident));
                continue;
            }

            // Unknown character fallback, treat as single char token or advance
            tokens.Add(new Token(TokenType.Identifier, c.ToString(), pos, c.ToString()));
            pos++;
        }

        tokens.Add(new Token(TokenType.EndOfFile, "", pos));
        return tokens;
    }

    private class FormulaParser
    {
        private readonly List<Token> _tokens;
        private int _current = 0;

        public FormulaParser(List<Token> tokens)
        {
            _tokens = tokens;
        }

        public bool IsAtEnd => Peek().Type == TokenType.EndOfFile;
        public Token CurrentToken => Peek();

        private Token Peek() => _tokens[_current];
        private Token Previous() => _tokens[_current - 1];

        private Token Advance()
        {
            if (!IsAtEnd) _current++;
            return Previous();
        }

        private bool Check(TokenType type)
        {
            if (IsAtEnd) return false;
            return Peek().Type == type;
        }

        private bool Match(params TokenType[] types)
        {
            foreach (var type in types)
            {
                if (Check(type))
                {
                    Advance();
                    return true;
                }
            }
            return false;
        }

        private Token Consume(TokenType type, string message)
        {
            if (Check(type)) return Advance();
            throw new InvalidOperationException($"{message} at position {Peek().Position}");
        }

        public FormulaAstNode ParseExpression()
        {
            return ParseLogicalOr();
        }

        private FormulaAstNode ParseLogicalOr()
        {
            var expr = ParseLogicalAnd();

            while (!IsAtEnd && Peek().Type == TokenType.Identifier && string.Equals(Peek().Text, "OR", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                var right = ParseLogicalAnd();
                expr = new BinaryOpNode("OR", expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseLogicalAnd()
        {
            var expr = ParseEquality();

            while (!IsAtEnd && Peek().Type == TokenType.Identifier && string.Equals(Peek().Text, "AND", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                var right = ParseEquality();
                expr = new BinaryOpNode("AND", expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseEquality()
        {
            var expr = ParseRelational();

            while (Match(TokenType.Equals, TokenType.DoubleEquals, TokenType.NotEquals))
            {
                var opToken = Previous();
                string op = opToken.Type is TokenType.Equals or TokenType.DoubleEquals ? "=" : "<>";
                var right = ParseRelational();
                expr = new BinaryOpNode(op, expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseRelational()
        {
            var expr = ParseConcat();

            while (Match(TokenType.LessThan, TokenType.LessThanOrEqual, TokenType.GreaterThan, TokenType.GreaterThanOrEqual))
            {
                var opToken = Previous();
                string op = opToken.Text;
                var right = ParseConcat();
                expr = new BinaryOpNode(op, expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseConcat()
        {
            var expr = ParseAdditive();

            while (Match(TokenType.Ampersand))
            {
                var right = ParseAdditive();
                expr = new BinaryOpNode("&", expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseAdditive()
        {
            var expr = ParseMultiplicative();

            while (Match(TokenType.Plus, TokenType.Minus))
            {
                string op = Previous().Text;
                var right = ParseMultiplicative();
                expr = new BinaryOpNode(op, expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseMultiplicative()
        {
            var expr = ParseUnary();

            while (Match(TokenType.Multiply, TokenType.Divide, TokenType.Modulo))
            {
                string op = Previous().Text;
                var right = ParseUnary();
                expr = new BinaryOpNode(op, expr, right);
            }

            return expr;
        }

        private FormulaAstNode ParseUnary()
        {
            if (Match(TokenType.Plus, TokenType.Minus))
            {
                string op = Previous().Text;
                var right = ParseUnary();
                return new UnaryOpNode(op, right);
            }

            if (Check(TokenType.Identifier) && string.Equals(Peek().Text, "NOT", StringComparison.OrdinalIgnoreCase))
            {
                Advance();
                var right = ParseUnary();
                return new UnaryOpNode("NOT", right);
            }

            return ParsePrimary();
        }

        private FormulaAstNode ParsePrimary()
        {
            if (Match(TokenType.Number))
            {
                return new LiteralNode(Previous().LiteralValue ?? 0.0);
            }

            if (Match(TokenType.String))
            {
                return new LiteralNode(Previous().LiteralValue ?? string.Empty);
            }

            if (Match(TokenType.BracketIdentifier))
            {
                return new ColumnRefNode(Previous().Text);
            }

            if (Match(TokenType.OpenParen))
            {
                var expr = ParseExpression();
                Consume(TokenType.CloseParen, "Expected ')' after expression");
                return expr;
            }

            if (Match(TokenType.Identifier))
            {
                string name = Previous().Text;

                // Function call: Identifier '('
                if (Match(TokenType.OpenParen))
                {
                    var args = new List<FormulaAstNode>();
                    if (!Check(TokenType.CloseParen))
                    {
                        do
                        {
                            args.Add(ParseExpression());
                        } while (Match(TokenType.Comma));
                    }
                    Consume(TokenType.CloseParen, $"Expected ')' after function arguments in {name}");
                    return new FunctionCallNode(name, args);
                }

                // Boolean literal
                if (string.Equals(name, "TRUE", StringComparison.OrdinalIgnoreCase))
                {
                    return new LiteralNode(true);
                }
                if (string.Equals(name, "FALSE", StringComparison.OrdinalIgnoreCase))
                {
                    return new LiteralNode(false);
                }

                // Bare Column or Excel letter reference (e.g. A, B, Col1, etc.)
                return new ColumnRefNode(name);
            }

            throw new InvalidOperationException($"Unexpected token '{Peek().Text}' at position {Peek().Position}");
        }
    }

    #endregion

    #region Evaluation & AST

    public class FormulaEvaluationContext
    {
        public IReadOnlyList<string> Columns { get; }
        public IReadOnlyList<string> RowValues { get; }
        public int RowIndex { get; }

        private readonly Dictionary<string, int> _columnNameToIndex = new(StringComparer.OrdinalIgnoreCase);

        public FormulaEvaluationContext(IReadOnlyList<string> columns, IReadOnlyList<string> rowValues, int rowIndex)
        {
            Columns = columns ?? Array.Empty<string>();
            RowValues = rowValues ?? Array.Empty<string>();
            RowIndex = rowIndex;

            for (int i = 0; i < Columns.Count; i++)
            {
                string col = Columns[i];
                if (!string.IsNullOrWhiteSpace(col) && !_columnNameToIndex.ContainsKey(col))
                {
                    _columnNameToIndex[col] = i;
                }
            }
        }

        public string GetColumnValue(string colRef)
        {
            if (string.IsNullOrWhiteSpace(colRef)) return string.Empty;
            string cleanRef = colRef.Trim();

            // 1. Direct name match in table columns (e.g. [FirstName], [Email], Price)
            if (_columnNameToIndex.TryGetValue(cleanRef, out int foundIdx))
            {
                return GetByIndex(foundIdx);
            }

            // 2. Excel column letter match (e.g. A, B, Z, AA, AB)
            int letterIdx = ColumnLetterToIndex(cleanRef);
            if (letterIdx >= 0 && letterIdx < Math.Max(Columns.Count, RowValues.Count))
            {
                return GetByIndex(letterIdx);
            }

            // 3. Dollar / Index match ($1, $2, Col1, Col2, 1, 2)
            if (cleanRef.StartsWith('$') && int.TryParse(cleanRef[1..], out int dollarIdx))
            {
                return GetByIndex(dollarIdx - 1);
            }
            if (cleanRef.StartsWith("Col", StringComparison.OrdinalIgnoreCase) && int.TryParse(cleanRef[3..], out int colNumIdx))
            {
                return GetByIndex(colNumIdx - 1);
            }
            if (cleanRef.StartsWith("Column_", StringComparison.OrdinalIgnoreCase) && int.TryParse(cleanRef[7..], out int colUnderscoreIdx))
            {
                return GetByIndex(colUnderscoreIdx - 1);
            }
            if (int.TryParse(cleanRef, out int rawNumIdx))
            {
                return GetByIndex(rawNumIdx - 1); // 1-based index
            }

            return string.Empty;
        }

        public string GetByIndex(int index)
        {
            if (index >= 0 && index < RowValues.Count)
            {
                return RowValues[index] ?? string.Empty;
            }
            return string.Empty;
        }

        private static int ColumnLetterToIndex(string colLetter)
        {
            if (string.IsNullOrEmpty(colLetter)) return -1;
            foreach (char c in colLetter)
            {
                if (!char.IsLetter(c)) return -1;
            }

            int result = 0;
            string upper = colLetter.ToUpperInvariant();
            for (int i = 0; i < upper.Length; i++)
            {
                result *= 26;
                result += (upper[i] - 'A' + 1);
            }
            return result - 1; // 0-based
        }
    }

    public abstract class FormulaAstNode
    {
        public abstract object? Evaluate(FormulaEvaluationContext context);
    }

    public class LiteralNode : FormulaAstNode
    {
        public object? Value { get; }
        public LiteralNode(object? value) => Value = value;

        public override object? Evaluate(FormulaEvaluationContext context) => Value;
    }

    public class ColumnRefNode : FormulaAstNode
    {
        public string ColumnRef { get; }
        public ColumnRefNode(string columnRef) => ColumnRef = columnRef;

        public override object? Evaluate(FormulaEvaluationContext context)
        {
            return context.GetColumnValue(ColumnRef);
        }
    }

    public class UnaryOpNode : FormulaAstNode
    {
        public string Operator { get; }
        public FormulaAstNode Operand { get; }

        public UnaryOpNode(string op, FormulaAstNode operand)
        {
            Operator = op;
            Operand = operand;
        }

        public override object? Evaluate(FormulaEvaluationContext context)
        {
            var val = Operand.Evaluate(context);
            if (Operator == "-")
            {
                double num = ToNumber(val);
                return -num;
            }
            if (Operator == "+")
            {
                return ToNumber(val);
            }
            if (Operator is "NOT" or "!")
            {
                return !ToBoolean(val);
            }
            return val;
        }
    }

    public class BinaryOpNode : FormulaAstNode
    {
        public string Operator { get; }
        public FormulaAstNode Left { get; }
        public FormulaAstNode Right { get; }

        public BinaryOpNode(string op, FormulaAstNode left, FormulaAstNode right)
        {
            Operator = op;
            Left = left;
            Right = right;
        }

        public override object? Evaluate(FormulaEvaluationContext context)
        {
            // Short-circuit logical operators
            if (string.Equals(Operator, "OR", StringComparison.OrdinalIgnoreCase))
            {
                bool leftBool = ToBoolean(Left.Evaluate(context));
                if (leftBool) return true;
                return ToBoolean(Right.Evaluate(context));
            }
            if (string.Equals(Operator, "AND", StringComparison.OrdinalIgnoreCase))
            {
                bool leftBool = ToBoolean(Left.Evaluate(context));
                if (!leftBool) return false;
                return ToBoolean(Right.Evaluate(context));
            }

            var leftVal = Left.Evaluate(context);
            var rightVal = Right.Evaluate(context);

            switch (Operator)
            {
                case "&":
                    return ToStringVal(leftVal) + ToStringVal(rightVal);

                case "+":
                    return ToNumber(leftVal) + ToNumber(rightVal);
                case "-":
                    return ToNumber(leftVal) - ToNumber(rightVal);
                case "*":
                    return ToNumber(leftVal) * ToNumber(rightVal);
                case "/":
                {
                    double denom = ToNumber(rightVal);
                    if (denom == 0) return double.NaN;
                    return ToNumber(leftVal) / denom;
                }
                case "%":
                {
                    double denom = ToNumber(rightVal);
                    if (denom == 0) return double.NaN;
                    return ToNumber(leftVal) % denom;
                }

                case "=":
                case "==":
                    return CompareValues(leftVal, rightVal) == 0;
                case "!=":
                case "<>":
                    return CompareValues(leftVal, rightVal) != 0;
                case "<":
                    return CompareValues(leftVal, rightVal) < 0;
                case "<=":
                    return CompareValues(leftVal, rightVal) <= 0;
                case ">":
                    return CompareValues(leftVal, rightVal) > 0;
                case ">=":
                    return CompareValues(leftVal, rightVal) >= 0;

                default:
                    return ToStringVal(leftVal);
            }
        }
    }

    public class FunctionCallNode : FormulaAstNode
    {
        public string Name { get; }
        public List<FormulaAstNode> Arguments { get; }

        public FunctionCallNode(string name, List<FormulaAstNode> arguments)
        {
            Name = name.ToUpperInvariant();
            Arguments = arguments;
        }

        public override object? Evaluate(FormulaEvaluationContext context)
        {
            return ExecuteFunction(Name, Arguments, context);
        }
    }

    #endregion

    #region Function Execution

    private static object? ExecuteFunction(string funcName, List<FormulaAstNode> args, FormulaEvaluationContext context)
    {
        switch (funcName)
        {
            // --- Text Combining & Splitting ---
            case "CONCAT":
            case "CONCATENATE":
            {
                var sb = new StringBuilder();
                foreach (var arg in args)
                {
                    sb.Append(ToStringVal(arg.Evaluate(context)));
                }
                return sb.ToString();
            }

            case "JOIN":
            {
                if (args.Count == 0) return string.Empty;
                string delim = ToStringVal(args[0].Evaluate(context));
                var items = new List<string>();
                for (int i = 1; i < args.Count; i++)
                {
                    items.Add(ToStringVal(args[i].Evaluate(context)));
                }
                return string.Join(delim, items);
            }

            case "TEXTJOIN":
            {
                if (args.Count < 2) return string.Empty;
                string delim = ToStringVal(args[0].Evaluate(context));
                bool ignoreEmpty = ToBoolean(args[1].Evaluate(context));
                var items = new List<string>();
                for (int i = 2; i < args.Count; i++)
                {
                    string str = ToStringVal(args[i].Evaluate(context));
                    if (!ignoreEmpty || !string.IsNullOrEmpty(str))
                    {
                        items.Add(str);
                    }
                }
                return string.Join(delim, items);
            }

            case "SPLIT":
            case "SPLIT_PART":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                string delimiter = ToStringVal(args[1].Evaluate(context));
                int index = args.Count > 2 ? (int)ToNumber(args[2].Evaluate(context)) : 1;

                if (string.IsNullOrEmpty(text)) return string.Empty;
                if (string.IsNullOrEmpty(delimiter))
                {
                    // Character split
                    if (index > 0 && index <= text.Length) return text[index - 1].ToString();
                    if (index < 0 && Math.Abs(index) <= text.Length) return text[text.Length + index].ToString();
                    return string.Empty;
                }

                var parts = text.Split(new[] { delimiter }, StringSplitOptions.None);
                if (index > 0 && index <= parts.Length)
                {
                    return parts[index - 1];
                }
                if (index < 0 && Math.Abs(index) <= parts.Length)
                {
                    return parts[parts.Length + index];
                }
                return string.Empty;
            }

            // --- Substrings & Slicing ---
            case "SUBSTRING":
            case "MID":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int start = (int)ToNumber(args[1].Evaluate(context)); // 1-based
                int length = args.Count > 2 ? (int)ToNumber(args[2].Evaluate(context)) : int.MaxValue;

                if (string.IsNullOrEmpty(text)) return string.Empty;
                if (start < 1) start = 1;
                int zeroStart = start - 1;
                if (zeroStart >= text.Length) return string.Empty;
                if (length <= 0) return string.Empty;

                int actualLen = Math.Min(length, text.Length - zeroStart);
                return text.Substring(zeroStart, actualLen);
            }

            case "LEFT":
            {
                if (args.Count == 0) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int numChars = args.Count > 1 ? (int)ToNumber(args[1].Evaluate(context)) : 1;
                if (string.IsNullOrEmpty(text) || numChars <= 0) return string.Empty;
                return numChars >= text.Length ? text : text[..numChars];
            }

            case "RIGHT":
            {
                if (args.Count == 0) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int numChars = args.Count > 1 ? (int)ToNumber(args[1].Evaluate(context)) : 1;
                if (string.IsNullOrEmpty(text) || numChars <= 0) return string.Empty;
                return numChars >= text.Length ? text : text.Substring(text.Length - numChars);
            }

            // --- Search, Find, IndexOf ---
            case "FIND":
            {
                // Case-sensitive search: FIND(find_text, within_text, [start_num])
                if (args.Count < 2) return 0;
                string findText = ToStringVal(args[0].Evaluate(context));
                string withinText = ToStringVal(args[1].Evaluate(context));
                int startNum = args.Count > 2 ? (int)ToNumber(args[2].Evaluate(context)) : 1;
                if (startNum < 1) startNum = 1;
                int zeroStart = Math.Min(startNum - 1, withinText.Length);

                int idx = withinText.IndexOf(findText, zeroStart, StringComparison.Ordinal);
                return idx >= 0 ? idx + 1 : 0;
            }

            case "SEARCH":
            case "INDEXOF":
            case "INSTR":
            {
                // Case-insensitive search: SEARCH(find_text, within_text, [start_num])
                if (args.Count < 2) return 0;
                string arg0 = ToStringVal(args[0].Evaluate(context));
                string arg1 = ToStringVal(args[1].Evaluate(context));
                int startNum = args.Count > 2 ? (int)ToNumber(args[2].Evaluate(context)) : 1;

                string findText, withinText;
                if (funcName == "INDEXOF")
                {
                    // Allow INDEXOF(within_text, find_text) or INDEXOF(find_text, within_text)
                    withinText = arg0;
                    findText = arg1;
                }
                else
                {
                    findText = arg0;
                    withinText = arg1;
                }

                if (startNum < 1) startNum = 1;
                int zeroStart = Math.Min(startNum - 1, withinText.Length);

                int idx = withinText.IndexOf(findText, zeroStart, StringComparison.OrdinalIgnoreCase);
                return idx >= 0 ? idx + 1 : 0;
            }

            case "CONTAINS":
            {
                if (args.Count < 2) return false;
                string text = ToStringVal(args[0].Evaluate(context));
                string sub = ToStringVal(args[1].Evaluate(context));
                bool caseSensitive = args.Count > 2 && ToBoolean(args[2].Evaluate(context));
                return text.Contains(sub, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }

            case "STARTSWITH":
            {
                if (args.Count < 2) return false;
                string text = ToStringVal(args[0].Evaluate(context));
                string prefix = ToStringVal(args[1].Evaluate(context));
                bool caseSensitive = args.Count > 2 && ToBoolean(args[2].Evaluate(context));
                return text.StartsWith(prefix, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }

            case "ENDSWITH":
            {
                if (args.Count < 2) return false;
                string text = ToStringVal(args[0].Evaluate(context));
                string suffix = ToStringVal(args[1].Evaluate(context));
                bool caseSensitive = args.Count > 2 && ToBoolean(args[2].Evaluate(context));
                return text.EndsWith(suffix, caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
            }

            // --- Replace & Substitute ---
            case "REPLACE":
            case "SUBSTITUTE":
            {
                if (args.Count < 3) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                string oldText = ToStringVal(args[1].Evaluate(context));
                string newText = ToStringVal(args[2].Evaluate(context));
                int instanceNum = args.Count > 3 ? (int)ToNumber(args[3].Evaluate(context)) : 0;

                if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(oldText))
                    return text;

                if (instanceNum <= 0)
                {
                    return text.Replace(oldText, newText, StringComparison.OrdinalIgnoreCase);
                }

                // Replace specific instance (1-based)
                int currentMatch = 0;
                int startPos = 0;
                while (startPos < text.Length)
                {
                    int found = text.IndexOf(oldText, startPos, StringComparison.OrdinalIgnoreCase);
                    if (found < 0) break;
                    currentMatch++;
                    if (currentMatch == instanceNum)
                    {
                        return text.Substring(0, found) + newText + text.Substring(found + oldText.Length);
                    }
                    startPos = found + oldText.Length;
                }
                return text;
            }

            // --- Regex Operations ---
            case "REGEXMATCH":
            case "REGEX_MATCH":
            case "ISREGEXMATCH":
            {
                if (args.Count < 2) return false;
                string text = ToStringVal(args[0].Evaluate(context));
                string pattern = ToStringVal(args[1].Evaluate(context));
                bool caseSensitive = args.Count > 2 && ToBoolean(args[2].Evaluate(context));

                try
                {
                    var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.IsMatch(text, pattern, options);
                }
                catch
                {
                    return false;
                }
            }

            case "REGEXEXTRACT":
            case "REGEX_EXTRACT":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                string pattern = ToStringVal(args[1].Evaluate(context));
                object? groupArg = args.Count > 2 ? args[2].Evaluate(context) : null;

                try
                {
                    var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase);
                    if (!match.Success) return string.Empty;

                    if (groupArg == null)
                    {
                        return match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    }

                    if (groupArg is int or long or double && int.TryParse(ToStringVal(groupArg), out int groupIndex))
                    {
                        if (groupIndex >= 0 && groupIndex < match.Groups.Count)
                        {
                            return match.Groups[groupIndex].Value;
                        }
                    }
                    else
                    {
                        string groupName = ToStringVal(groupArg);
                        if (int.TryParse(groupName, out int parsedIdx))
                        {
                            if (parsedIdx >= 0 && parsedIdx < match.Groups.Count)
                                return match.Groups[parsedIdx].Value;
                        }
                        var grp = match.Groups[groupName];
                        if (grp.Success) return grp.Value;
                    }

                    return match.Value;
                }
                catch
                {
                    return string.Empty;
                }
            }

            case "REGEXREPLACE":
            case "REGEX_REPLACE":
            {
                if (args.Count < 3) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                string pattern = ToStringVal(args[1].Evaluate(context));
                string replacement = ToStringVal(args[2].Evaluate(context));

                try
                {
                    return Regex.Replace(text, pattern, replacement, RegexOptions.IgnoreCase);
                }
                catch
                {
                    return text;
                }
            }

            // --- Casing & Formatting ---
            case "UPPER":
            case "UPPERCASE":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).ToUpperInvariant() : string.Empty;

            case "LOWER":
            case "LOWERCASE":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).ToLowerInvariant() : string.Empty;

            case "PROPER":
            case "TITLE":
            case "TITLECASE":
                return args.Count > 0 ? CaseTransformers.ChangeCase(ToStringVal(args[0].Evaluate(context)), TextCasing.TitleCase) : string.Empty;

            case "TRIM":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).Trim() : string.Empty;

            case "LTRIM":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).TrimStart() : string.Empty;

            case "RTRIM":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).TrimEnd() : string.Empty;

            case "LEN":
            case "LENGTH":
                return args.Count > 0 ? ToStringVal(args[0].Evaluate(context)).Length : 0;

            case "PADLEFT":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int width = (int)ToNumber(args[1].Evaluate(context));
                char padChar = args.Count > 2 ? ToStringVal(args[2].Evaluate(context)).FirstOrDefault(' ') : ' ';
                return width > text.Length ? text.PadLeft(width, padChar) : text;
            }

            case "PADRIGHT":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int width = (int)ToNumber(args[1].Evaluate(context));
                char padChar = args.Count > 2 ? ToStringVal(args[2].Evaluate(context)).FirstOrDefault(' ') : ' ';
                return width > text.Length ? text.PadRight(width, padChar) : text;
            }

            case "REPEAT":
            case "REPT":
            {
                if (args.Count < 2) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                int count = (int)ToNumber(args[1].Evaluate(context));
                if (count <= 0 || string.IsNullOrEmpty(text)) return string.Empty;
                var sb = new StringBuilder(text.Length * Math.Min(count, 1000));
                for (int i = 0; i < count; i++) sb.Append(text);
                return sb.ToString();
            }

            case "REVERSE":
            {
                if (args.Count == 0) return string.Empty;
                string text = ToStringVal(args[0].Evaluate(context));
                var charArray = text.ToCharArray();
                Array.Reverse(charArray);
                return new string(charArray);
            }

            // --- Logic & Control Flow ---
            case "IF":
            {
                if (args.Count < 2) return string.Empty;
                bool condition = ToBoolean(args[0].Evaluate(context));
                if (condition)
                {
                    return args[1].Evaluate(context);
                }
                return args.Count > 2 ? args[2].Evaluate(context) : string.Empty;
            }

            case "IFS":
            {
                // IFS(cond1, val1, cond2, val2, ...)
                for (int i = 0; i < args.Count; i += 2)
                {
                    bool condition = ToBoolean(args[i].Evaluate(context));
                    if (condition)
                    {
                        return i + 1 < args.Count ? args[i + 1].Evaluate(context) : string.Empty;
                    }
                }
                return string.Empty;
            }

            case "SWITCH":
            {
                // SWITCH(expr, val1, res1, val2, res2, ..., [default])
                if (args.Count < 3) return string.Empty;
                var target = args[0].Evaluate(context);
                for (int i = 1; i + 1 < args.Count; i += 2)
                {
                    var caseVal = args[i].Evaluate(context);
                    if (CompareValues(target, caseVal) == 0)
                    {
                        return args[i + 1].Evaluate(context);
                    }
                }
                // Default value if odd arguments count
                if (args.Count % 2 == 0)
                {
                    return args[^1].Evaluate(context);
                }
                return string.Empty;
            }

            case "IFERROR":
            {
                if (args.Count < 2) return string.Empty;
                try
                {
                    var val = args[0].Evaluate(context);
                    string strVal = ToStringVal(val);
                    if (strVal.StartsWith("#ERROR") || strVal == "#NUM!" || strVal == "#VALUE!")
                    {
                        return args[1].Evaluate(context);
                    }
                    if (val is double d && (double.IsNaN(d) || double.IsInfinity(d)))
                    {
                        return args[1].Evaluate(context);
                    }
                    return val;
                }
                catch
                {
                    return args[1].Evaluate(context);
                }
            }

            case "COALESCE":
            case "IFBLANK":
            {
                foreach (var arg in args)
                {
                    var val = arg.Evaluate(context);
                    if (val != null && !string.IsNullOrWhiteSpace(ToStringVal(val)))
                    {
                        return val;
                    }
                }
                return string.Empty;
            }

            case "ISBLANK":
            case "ISEMPTY":
            {
                if (args.Count == 0) return true;
                var val = args[0].Evaluate(context);
                return val == null || string.IsNullOrWhiteSpace(ToStringVal(val));
            }

            case "NOT":
                return args.Count > 0 && !ToBoolean(args[0].Evaluate(context));

            case "AND":
            {
                if (args.Count == 0) return false;
                foreach (var arg in args)
                {
                    if (!ToBoolean(arg.Evaluate(context))) return false;
                }
                return true;
            }

            case "OR":
            {
                if (args.Count == 0) return false;
                foreach (var arg in args)
                {
                    if (ToBoolean(arg.Evaluate(context))) return true;
                }
                return false;
            }

            // --- Math & Numeric ---
            case "ROUND":
            {
                if (args.Count == 0) return 0.0;
                double num = ToNumber(args[0].Evaluate(context));
                int decimals = args.Count > 1 ? (int)ToNumber(args[1].Evaluate(context)) : 0;
                return Math.Round(num, Math.Max(0, decimals), MidpointRounding.AwayFromZero);
            }

            case "INT":
            case "FLOOR":
            {
                if (args.Count == 0) return 0.0;
                return Math.Floor(ToNumber(args[0].Evaluate(context)));
            }

            case "CEILING":
            case "CEIL":
            {
                if (args.Count == 0) return 0.0;
                return Math.Ceiling(ToNumber(args[0].Evaluate(context)));
            }

            case "ABS":
                return args.Count > 0 ? Math.Abs(ToNumber(args[0].Evaluate(context))) : 0.0;

            case "MOD":
            {
                if (args.Count < 2) return 0.0;
                double num = ToNumber(args[0].Evaluate(context));
                double div = ToNumber(args[1].Evaluate(context));
                return div != 0 ? num % div : double.NaN;
            }

            case "SUM":
            {
                double sum = 0;
                foreach (var arg in args)
                {
                    sum += ToNumber(arg.Evaluate(context));
                }
                return sum;
            }

            case "AVERAGE":
            case "AVG":
            {
                if (args.Count == 0) return 0.0;
                double sum = 0;
                foreach (var arg in args)
                {
                    sum += ToNumber(arg.Evaluate(context));
                }
                return sum / args.Count;
            }

            case "MIN":
            {
                if (args.Count == 0) return 0.0;
                double min = double.MaxValue;
                foreach (var arg in args)
                {
                    min = Math.Min(min, ToNumber(arg.Evaluate(context)));
                }
                return min;
            }

            case "MAX":
            {
                if (args.Count == 0) return 0.0;
                double max = double.MinValue;
                foreach (var arg in args)
                {
                    max = Math.Max(max, ToNumber(arg.Evaluate(context)));
                }
                return max;
            }

            // --- Utility / Column references ---
            case "ROW":
                return context.RowIndex + 1; // 1-based row number

            case "COL":
            case "COLUMN":
            {
                if (args.Count == 0) return string.Empty;
                string colRef = ToStringVal(args[0].Evaluate(context));
                return context.GetColumnValue(colRef);
            }

            default:
                throw new InvalidOperationException($"Unknown function '{funcName}'");
        }
    }

    #endregion

    #region Value Conversions & Comparison

    private static string ToStringVal(object? val)
    {
        if (val == null) return string.Empty;
        if (val is bool b) return b ? "TRUE" : "FALSE";
        if (val is double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) return "#NUM!";
            return d % 1 == 0 ? ((long)d).ToString(CultureInfo.InvariantCulture) : d.ToString("G", CultureInfo.InvariantCulture);
        }
        return val.ToString() ?? string.Empty;
    }

    private static double ToNumber(object? val)
    {
        if (val == null) return 0;
        if (val is double d) return d;
        if (val is int i) return i;
        if (val is long l) return l;
        if (val is bool b) return b ? 1.0 : 0.0;
        string str = val.ToString()?.Trim() ?? string.Empty;
        if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out double res))
        {
            return res;
        }
        return 0.0;
    }

    private static bool ToBoolean(object? val)
    {
        if (val == null) return false;
        if (val is bool b) return b;
        if (val is double d) return d != 0 && !double.IsNaN(d);
        if (val is int i) return i != 0;
        string str = val.ToString()?.Trim() ?? string.Empty;
        if (string.Equals(str, "TRUE", StringComparison.OrdinalIgnoreCase) || str == "1" || string.Equals(str, "YES", StringComparison.OrdinalIgnoreCase))
            return true;
        if (string.Equals(str, "FALSE", StringComparison.OrdinalIgnoreCase) || str == "0" || string.Equals(str, "NO", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(str))
            return false;
        return true;
    }

    private static int CompareValues(object? a, object? b)
    {
        if (a == null && b == null) return 0;
        if (a == null) return -1;
        if (b == null) return 1;

        if (a is bool aBool && b is bool bBool)
        {
            return aBool.CompareTo(bBool);
        }

        string aStr = ToStringVal(a).Trim();
        string bStr = ToStringVal(b).Trim();

        if (double.TryParse(aStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double aNum) &&
            double.TryParse(bStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double bNum))
        {
            return aNum.CompareTo(bNum);
        }

        return string.Compare(aStr, bStr, StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}
