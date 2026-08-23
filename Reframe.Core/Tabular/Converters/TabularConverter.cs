using Reframe.Core.Tabular.Models;

namespace Reframe.Core.Tabular.Converters;

public static class TabularConverter
{
    public static ITabularConverter Instance { get; set; } = TabularConverterService.Instance;

    public static string ToCsv(TabularData table, char delimiter = ',') => Instance.ToCsv(table, delimiter);
    public static string ToTsv(TabularData table) => Instance.ToTsv(table);
    public static string ToMarkdownTable(TabularData table) => Instance.ToMarkdownTable(table);
    public static string ToJsonArrayOfObjects(TabularData table, bool indented = true) => Instance.ToJsonArrayOfObjects(table, indented);
    public static string ToJsonArrayOfArrays(TabularData table, bool indented = true) => Instance.ToJsonArrayOfArrays(table, indented);
    public static string ToSqlInsertStatements(TabularData table, string tableName = "MyTable") => Instance.ToSqlInsertStatements(table, tableName);
    public static string ToHtmlTable(TabularData table) => Instance.ToHtmlTable(table);
    public static string ToKeyValueJson(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false, bool indented = true) => Instance.ToKeyValueJson(table, keyColIndex, valueColIndex, includeRestOfColumns, indented);
    public static string ToKeyValueJson(TabularData table, int keyColIndex, bool includeRestOfColumns, bool indented = true) => Instance.ToKeyValueJson(table, keyColIndex, includeRestOfColumns, indented);
    public static string ToYaml(TabularData table) => Instance.ToYaml(table);
    public static string ToYamlArrays(TabularData table) => Instance.ToYamlArrays(table);
    public static string ToKeyValueYaml(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false) => Instance.ToKeyValueYaml(table, keyColIndex, valueColIndex, includeRestOfColumns);
    public static string ToKeyValueYaml(TabularData table, int keyColIndex, bool includeRestOfColumns) => Instance.ToKeyValueYaml(table, keyColIndex, includeRestOfColumns);
    public static string ToKeyValueQueryString(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false) => Instance.ToKeyValueQueryString(table, keyColIndex, valueColIndex, includeRestOfColumns);
    public static string ToKeyValueQueryString(TabularData table, int keyColIndex, bool includeRestOfColumns) => Instance.ToKeyValueQueryString(table, keyColIndex, includeRestOfColumns);
    public static string ToSqlInClause(TabularData table, int colIndex, bool quoteStrings = true) => Instance.ToSqlInClause(table, colIndex, quoteStrings);
}
