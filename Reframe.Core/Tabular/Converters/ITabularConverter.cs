namespace Reframe.Core.Tabular;

public interface ITabularConverter
{
    string ToCsv(TabularData table, char delimiter = ',');
    string ToTsv(TabularData table);
    string ToMarkdownTable(TabularData table);
    string ToJsonArrayOfObjects(TabularData table, bool indented = true);
    string ToJsonArrayOfArrays(TabularData table, bool indented = true);
    string ToSqlInsertStatements(TabularData table, string tableName = "MyTable");
    string ToHtmlTable(TabularData table);
    string ToKeyValueJson(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false, bool indented = true);
    string ToKeyValueJson(TabularData table, int keyColIndex, bool includeRestOfColumns, bool indented = true);
    string ToYaml(TabularData table);
    string ToYamlArrays(TabularData table);
    string ToKeyValueYaml(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false);
    string ToKeyValueYaml(TabularData table, int keyColIndex, bool includeRestOfColumns);
    string ToKeyValueQueryString(TabularData table, int keyColIndex, int valueColIndex, bool includeRestOfColumns = false);
    string ToKeyValueQueryString(TabularData table, int keyColIndex, bool includeRestOfColumns);
    string ToSqlInClause(TabularData table, int colIndex, bool quoteStrings = true);
}
