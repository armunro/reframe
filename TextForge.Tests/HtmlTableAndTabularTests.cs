using TextForge.Core.Analysis;
using TextForge.Core.Tabular;
using TextForge.Core.Transformers;
using Xunit;

namespace TextForge.Tests;

public class HtmlTableAndTabularTests
{
    [Fact]
    public void IsHtmlTable_IdentifiesHtmlTableStructures()
    {
        string confluenceHtml = @"<table class=""confluenceTable"">
            <thead><tr><th>ID</th><th>Name</th></tr></thead>
            <tbody><tr><td>1</td><td>Alice</td></tr></tbody>
        </table>";

        string rawTable = "<table><tr><td>A</td><td>B</td></tr></table>";
        string excelHtml = "<!--StartFragment--><table><tr><td class=xl65>Item</td></tr></table><!--EndFragment-->";
        string nonTable = "Just some text\nwith multiple lines";

        Assert.True(HtmlTableParser.IsHtmlTable(confluenceHtml));
        Assert.True(HtmlTableParser.IsHtmlTable(rawTable));
        Assert.True(HtmlTableParser.IsHtmlTable(excelHtml));
        Assert.False(HtmlTableParser.IsHtmlTable(nonTable));
    }

    [Fact]
    public void Parse_ConfluenceHtmlTable_ExtractsColumnsAndRowsCorrectly()
    {
        string confluenceTable = @"
<table class=""confluenceTable"">
  <colgroup><col/><col/><col/><col/></colgroup>
  <thead>
    <tr class=""headerRow"">
      <th class=""confluenceTh""><p>Emp ID</p></th>
      <th class=""confluenceTh""><p>Full Name</p></th>
      <th class=""confluenceTh""><p>Role &amp; Title</p></th>
      <th class=""confluenceTh""><p>Salary</p></th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td class=""confluenceTd""><p>101</p></td>
      <td class=""confluenceTd""><a href=""/users/alice"">Alice &amp; Bob</a></td>
      <td class=""confluenceTd""><span>Lead Architect</span></td>
      <td class=""confluenceTd"">$140,000&nbsp;USD</td>
    </tr>
    <tr>
      <td class=""confluenceTd""><p>102</p></td>
      <td class=""confluenceTd""><a href=""/users/charlie"">Charlie Brown</a></td>
      <td class=""confluenceTd"">Senior Developer</td>
      <td class=""confluenceTd"">$115,000 USD</td>
    </tr>
  </tbody>
</table>";

        var table = HtmlTableParser.Parse(confluenceTable);

        Assert.NotNull(table);
        Assert.Equal(4, table.Columns.Count);
        Assert.Equal("Emp ID", table.Columns[0]);
        Assert.Equal("Full Name", table.Columns[1]);
        Assert.Equal("Role & Title", table.Columns[2]);
        Assert.Equal("Salary", table.Columns[3]);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("101", table.Rows[0][0]);
        Assert.Equal("Alice & Bob", table.Rows[0][1]);
        Assert.Equal("Lead Architect", table.Rows[0][2]);
        Assert.Equal("$140,000 USD", table.Rows[0][3]);

        Assert.Equal("102", table.Rows[1][0]);
        Assert.Equal("Charlie Brown", table.Rows[1][1]);
    }

    [Fact]
    public void Parse_ExcelHtmlClipboard_ExtractsDataCleanly()
    {
        string excelHtml = @"
<html xmlns:o=""urn:schemas-microsoft-com:office:office"" xmlns:x=""urn:schemas-microsoft-com:office:excel"">
<body>
<!--StartFragment-->
<table border=0 cellpadding=0 cellspacing=0 width=256 style='border-collapse: collapse;table-layout:fixed;width:192pt'>
 <tr height=20 style='height:15.0pt'>
  <td height=20 class=xl65 width=64 style='height:15.0pt;width:48pt'>Product</td>
  <td class=xl65 width=64 style='width:48pt'>SKU</td>
  <td class=xl65 width=64 style='width:48pt'>Price</td>
 </tr>
 <tr height=20 style='height:15.0pt'>
  <td height=20 class=xl66 style='height:15.0pt'>Laptop Pro</td>
  <td class=xl66>LP-9000</td>
  <td class=xl67 align=right>1299.99</td>
 </tr>
 <tr height=20 style='height:15.0pt'>
  <td height=20 class=xl66 style='height:15.0pt'>Wireless Mouse</td>
  <td class=xl66>WM-400</td>
  <td class=xl67 align=right>29.50</td>
 </tr>
</table>
<!--EndFragment-->
</body>
</html>";

        var table = HtmlTableParser.Parse(excelHtml);

        Assert.NotNull(table);
        Assert.Equal(3, table.Columns.Count);
        Assert.Equal("Product", table.Columns[0]);
        Assert.Equal("SKU", table.Columns[1]);
        Assert.Equal("Price", table.Columns[2]);

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal("Laptop Pro", table.Rows[0][0]);
        Assert.Equal("LP-9000", table.Rows[0][1]);
        Assert.Equal("1299.99", table.Rows[0][2]);
    }

    [Fact]
    public void TextAnalyzer_DetectsHtmlTable()
    {
        string html = "<table><tr><th>ID</th><th>Name</th></tr><tr><td>1</td><td>Test</td></tr></table>";
        var result = TextAnalyzer.Analyze(html);

        Assert.Equal(DetectedFormat.HtmlTable, result.Format);
        Assert.True(result.IsTabular);
        Assert.Equal(2, result.ColumnCount);
        Assert.Equal(1, result.RowCount);
    }

    [Fact]
    public void TabularData_SelectColumns_ExtractsOnlySelectedColumns()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "ID", "First", "Last", "Email", "Department" },
            Rows = new List<List<string>>
            {
                new() { "1", "Alice", "Smith", "alice@test.com", "Engineering" },
                new() { "2", "Bob", "Jones", "bob@test.com", "Marketing" }
            }
        };

        // Select ID (col 0) and Email (col 3)
        var subTable = table.SelectColumns(new[] { 0, 3 });

        Assert.Equal(2, subTable.Columns.Count);
        Assert.Equal("ID", subTable.Columns[0]);
        Assert.Equal("Email", subTable.Columns[1]);

        Assert.Equal("1", subTable.Rows[0][0]);
        Assert.Equal("alice@test.com", subTable.Rows[0][1]);
        Assert.Equal("2", subTable.Rows[1][0]);
        Assert.Equal("bob@test.com", subTable.Rows[1][1]);
    }

    [Fact]
    public void TabularData_ExtractColumnsAsLines_JoinsWithDelimiter()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Col1", "Col2", "Col3" },
            Rows = new List<List<string>>
            {
                new() { "A", "B", "C" },
                new() { "D", "E", "F" }
            }
        };

        var lines = table.ExtractColumnsAsLines(new[] { 0, 2 }, " - ");
        Assert.Equal(2, lines.Count);
        Assert.Equal("A - C", lines[0]);
        Assert.Equal("D - F", lines[1]);
    }

    [Fact]
    public void TabularData_TransformColumns_AppliesFunctionToTargetColumns()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Name", "City", "Code" },
            Rows = new List<List<string>>
            {
                new() { "alice", "new york", "ny" },
                new() { "bob", "san francisco", "sf" }
            }
        };

        var upperTable = table.TransformColumns(new[] { 0, 2 }, s => s.ToUpperInvariant());

        Assert.Equal("ALICE", upperTable.Rows[0][0]);
        Assert.Equal("new york", upperTable.Rows[0][1]); // Untouched
        Assert.Equal("NY", upperTable.Rows[0][2]);

        Assert.Equal("BOB", upperTable.Rows[1][0]);
        Assert.Equal("san francisco", upperTable.Rows[1][1]);
        Assert.Equal("SF", upperTable.Rows[1][2]);
    }

    [Fact]
    public void TabularData_SortByColumn_SortsRowsCorrectly()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Name", "Score" },
            Rows = new List<List<string>>
            {
                new() { "Item 10", "100" },
                new() { "Item 2", "20" },
                new() { "Item 1", "10" }
            }
        };

        var sortedAsc = table.SortByColumn(0, SortOrder.NaturalNumericAsc);
        Assert.Equal("Item 1", sortedAsc.Rows[0][0]);
        Assert.Equal("Item 2", sortedAsc.Rows[1][0]);
        Assert.Equal("Item 10", sortedAsc.Rows[2][0]);
    }

    [Fact]
    public void TabularData_FilterRows_FiltersMatchingQuery()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Name", "Role" },
            Rows = new List<List<string>>
            {
                new() { "Alice", "Developer" },
                new() { "Bob", "Designer" },
                new() { "Charlie", "Senior Developer" }
            }
        };

        var filtered = table.FilterRows(1, "Developer");
        Assert.Equal(2, filtered.Rows.Count);
        Assert.Equal("Alice", filtered.Rows[0][0]);
        Assert.Equal("Charlie", filtered.Rows[1][0]);
    }

    [Fact]
    public void TabularConverter_ToKeyValueJson_GeneratesValidJsonMap()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "Key", "Value" },
            Rows = new List<List<string>>
            {
                new() { "apiHost", "https://api.example.com" },
                new() { "timeout", "30" }
            }
        };

        string json = TabularConverter.ToKeyValueJson(table, 0, 1, indented: false);
        Assert.Contains("\"apiHost\":\"https://api.example.com\"", json);
        Assert.Contains("\"timeout\":\"30\"", json);
    }

    [Fact]
    public void TabularConverter_ToSqlInClause_GeneratesSqlInFromColumn()
    {
        var table = new TabularData
        {
            Columns = new List<string> { "ID", "Name" },
            Rows = new List<List<string>>
            {
                new() { "USR-001", "Alice" },
                new() { "USR-002", "Bob" },
                new() { "USR-003", "Charlie" }
            }
        };

        string sqlIn = TabularConverter.ToSqlInClause(table, 0, quoteStrings: true);
        Assert.Equal("IN ('USR-001', 'USR-002', 'USR-003')", sqlIn);
    }

    [Fact]
    public void ExtractTableHtml_ExtractsCleanTableFromClipboardHtml()
    {
        string clipboardHtml = @"Version:0.9
StartHTML:0000000105
EndHTML:0000000789
StartFragment:0000000543
EndFragment:0000000753
<html><body>
<!--StartFragment--><table class=""confluenceTable""><tbody><tr><th>Col1</th><th>Col2</th></tr><tr><td>Val1</td><td>Val2</td></tr></tbody></table><!--EndFragment-->
</body></html>";

        string extracted = HtmlTableParser.ExtractTableHtml(clipboardHtml);
        Assert.StartsWith("<table", extracted);
        Assert.EndsWith("</table>", extracted);
        Assert.DoesNotContain("Version:0.9", extracted);
        Assert.DoesNotContain("<!--StartFragment-->", extracted);

        var parsed = TabularParser.DetectAndParse(extracted);
        Assert.NotNull(parsed);
        Assert.Equal(2, parsed.Columns.Count);
        Assert.Equal("Col1", parsed.Columns[0]);
        Assert.Equal("Col2", parsed.Columns[1]);
        Assert.Single(parsed.Rows);
        Assert.Equal("Val1", parsed.Rows[0][0]);
    }

    [Fact]
    public void TextAnalyzer_DetectsConfluenceAndExcelPastedDataAsTabular()
    {
        string confluenceHtml = @"<table class=""confluenceTable"">
            <tr><th>Server</th><th>IP</th></tr>
            <tr><td>app-1</td><td>10.0.0.1</td></tr>
        </table>";

        string excelTsv = "Name\tDepartment\tSalary\nAlice\tEngineering\t120000\nBob\tDesign\t95000";

        var analysisHtml = TextAnalyzer.Analyze(confluenceHtml);
        Assert.Equal(DetectedFormat.HtmlTable, analysisHtml.Format);
        Assert.True(analysisHtml.IsTabular);
        Assert.Equal(2, analysisHtml.ColumnCount);

        var analysisTsv = TextAnalyzer.Analyze(excelTsv);
        Assert.Equal(DetectedFormat.TsvTable, analysisTsv.Format);
        Assert.True(analysisTsv.IsTabular);
        Assert.Equal(3, analysisTsv.ColumnCount);
    }

    [Fact]
    public void Parse_ProseMirrorHtmlTable_WithSlashInHeader_ParsesAllColumnsCorrectly()
    {
        string proseMirrorTable = @"<table data-number-column=""false"" data-layout=""align-start"" data-autosize=""false"" data-table-local-id=""c0160465daa1"" data-table-width=""999"" data-ssr-placeholder=""table-c0160465daa1"" data-ssr-placeholder-replace=""table-c0160465daa1""><colgroup><col style=""width: max(calc(222.8px * 0.6), calc(222.8 * calc(calc(var(--ak-editor-table-width) - 1px)/999)), 48px)""><col style=""width: max(calc(119.8px * 0.6), calc(119.8 * calc(calc(var(--ak-editor-table-width) - 1px)/999)), 48px)""><col style=""width: max(calc(170.8px * 0.6), calc(170.8 * calc(calc(var(--ak-editor-table-width) - 1px)/999)), 48px)""><col style=""width: max(calc(338.8px * 0.6), calc(338.8 * calc(calc(var(--ak-editor-table-width) - 1px)/999)), 48px)""><col style=""width: max(calc(145.8px * 0.6), calc(145.8 * calc(calc(var(--ak-editor-table-width) - 1px)/999)), 48px)""></colgroup><tbody><tr data-local-id=""488511ad-9e3b-4ccf-8048-f6e71d7b0ada"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableRow"" data-prosemirror-node-block=""true""><th data-colwidth=""223"" class=""pm-table-header-content-wrap"" data-local-id=""c8368b9d-46e9-46f1-bdea-5460eb639e82"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableHeader"" data-prosemirror-node-block=""true""><p data-local-id=""a962884f0eaf"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""></p><p data-local-id=""edfdabdeedfb"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong"">Client Name</strong></p></th><th data-colwidth=""120"" class=""pm-table-header-content-wrap"" data-local-id=""085abdfd-deb4-4abc-858f-de9d28fe8f4d"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableHeader"" data-prosemirror-node-block=""true""><p data-local-id=""e469c2f692e6"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong""> yCRM Pin</strong></p></th><th data-colwidth=""171"" class=""pm-table-header-content-wrap"" data-local-id=""68840c1c97ab"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableHeader"" data-prosemirror-node-block=""true""><p data-local-id=""e1f8bccec849"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong"">Primary/Stellar Pin</strong></p></th><th data-colwidth=""339"" class=""pm-table-header-content-wrap"" data-local-id=""6a2377cc-07ad-44d5-87de-449d595b2eba"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableHeader"" data-prosemirror-node-block=""true""><p data-local-id=""6368b5ae3bc9"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong"">Y1 Tenant</strong></p></th><th data-colwidth=""146"" class=""pm-table-header-content-wrap"" data-local-id=""911a20e8c904"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableHeader"" data-prosemirror-node-block=""true""><p data-local-id=""95976446f134"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong"">App added to Y1 Dashboard</strong></p></th></tr><tr data-local-id=""fba974b0-fa7b-41ba-b2a5-fdd0e68e94ef"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableRow"" data-prosemirror-node-block=""true""><td data-colwidth=""223"" class=""pm-table-cell-content-wrap"" data-local-id=""0fc9a57d-3107-4c29-8c5b-026b1d3271f7"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableCell"" data-prosemirror-node-block=""true""><p data-local-id=""dc38ecd6c52e"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true"">Raleigh Housing Authority</p></td><td data-colwidth=""120"" class=""pm-table-cell-content-wrap"" data-local-id=""937d9cf2-e47d-472a-b946-4853966ebec3"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableCell"" data-prosemirror-node-block=""true""><p data-local-id=""d100f4dac9a6"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true"">100123201</p></td><td data-colwidth=""171"" class=""pm-table-cell-content-wrap"" data-local-id=""ba5596bf70a2"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableCell"" data-prosemirror-node-block=""true""><p data-local-id=""8b1fea8aaba9"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true"">100110792</p></td><td data-colwidth=""339"" class=""pm-table-cell-content-wrap"" data-local-id=""10fcfda3-79a5-4e79-a2b2-946ee121a6eb"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableCell"" data-prosemirror-node-block=""true""><p data-local-id=""1a877ae351e9"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""paragraph"" data-prosemirror-node-block=""true""><a href=""https://raleighha.yardione.com/"" data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""link""><strong data-prosemirror-content-type=""mark"" data-prosemirror-mark-name=""strong"">https://raleighha.yardione.com</strong></a></p></td><td data-colwidth=""146"" class=""pm-table-cell-content-wrap"" data-local-id=""0cc5600f24c3"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""tableCell"" data-prosemirror-node-block=""true""><div data-node-type=""actionList"" data-task-list-local-id=""9ae2c716-40a3-4664-bf6e-53f16d8e034a"" style=""list-style: none; padding-left: 0"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""taskList"" data-prosemirror-node-block=""true""><div class=""taskItemView-content-wrap"" data-task-local-id=""a05cc890-cf13-4b83-8ea3-45d51dddaabb"" data-task-state=""DONE"" style=""line-height: 24px; list-style-type: none; min-width: 48px; position: relative;"" data-prosemirror-content-type=""node"" data-prosemirror-node-name=""taskItem"" data-prosemirror-node-block=""true""><div style=""display: flex;""><span contenteditable=""false"" style=""display: grid; height: 24px; line-height: 24px; place-content: center center; width: 24px;""><input name=""a05cc890-cf13-4b83-8ea3-45d51dddaabb"" id=""a05cc890-cf13-4b83-8ea3-45d51dddaabb"" type=""checkbox"" checked=""true"" data-input-type=""lazy-task-item"" style=""accent-color: var(--ds-background-selected-bold, #1868DB); height: 13px; margin: 1px 0 0 0; padding: 0; width: 13px;""></span><div data-component=""content""><div class=""task-item"" style=""color: var(--ds-text, #292A2E); display: block; font-family: var(--ds-font-body, normal 400 14px/20px &quot;Atlassian Sans&quot;, ui-sans-serif, -apple-system, BlinkMacSystemFont, &quot;Segoe UI&quot;, Ubuntu, &quot;Helvetica Neue&quot;, sans-serif); font-size: 16px;""></div></div></div></div></div></td></tr></tbody></table>";

        var table = HtmlTableParser.Parse(proseMirrorTable);

        Assert.NotNull(table);
        Assert.Equal(5, table.Columns.Count);
        Assert.Equal("Client Name", table.Columns[0]);
        Assert.Equal("yCRM Pin", table.Columns[1]);
        Assert.Equal("Primary/Stellar Pin", table.Columns[2]);
        Assert.Equal("Y1 Tenant", table.Columns[3]);
        Assert.Equal("App added to Y1 Dashboard", table.Columns[4]);

        Assert.Single(table.Rows);
        Assert.Equal("Raleigh Housing Authority", table.Rows[0][0]);
        Assert.Equal("100123201", table.Rows[0][1]);
        Assert.Equal("100110792", table.Rows[0][2]);
        Assert.Equal("https://raleighha.yardione.com", table.Rows[0][3]);
    }

    [Fact]
    public void TabularParser_DetectAndParse_HandlesSqlInClauseAndSpecialHeaderNames()
    {
        string sqlInOutput = "IN (1001, 1002, 1003, 1004, 1005)";
        
        // Auto-detect identifies this as data without headers (contains numeric values)
        var autoParsed = TabularParser.DetectAndParse(sqlInOutput);
        Assert.NotNull(autoParsed);
        Assert.False(autoParsed.HasHeaders);
        Assert.Equal(5, autoParsed.Columns.Count);
        Assert.Equal("Column 1", autoParsed.Columns[0]);
        Assert.Single(autoParsed.Rows);
        Assert.Equal("IN (1001", autoParsed.Rows[0][0]);
        Assert.Equal("1002", autoParsed.Rows[0][1]);
        Assert.Equal("1005)", autoParsed.Rows[0][4]);

        // Explicit assumeHeader: true uses row 0 as headers even with special characters / parentheses
        var explicitHeaderParsed = TabularParser.DetectAndParse(sqlInOutput, assumeHeader: true);
        Assert.NotNull(explicitHeaderParsed);
        Assert.True(explicitHeaderParsed.HasHeaders);
        Assert.Equal(5, explicitHeaderParsed.Columns.Count);
        Assert.Equal("IN (1001", explicitHeaderParsed.Columns[0]);
        Assert.Equal("1002", explicitHeaderParsed.Columns[1]);
        Assert.Equal("1003", explicitHeaderParsed.Columns[2]);
        Assert.Equal("1004", explicitHeaderParsed.Columns[3]);
        Assert.Equal("1005)", explicitHeaderParsed.Columns[4]);
    }

    [Fact]
    public void TabularParser_AutodetectsHeaderPresence_Correctly()
    {
        // 1. CSV with clear headers (text headers + numeric/date/typed data rows)
        string withHeadersCsv = "ID,Name,Salary\n1,Alice,120000\n2,Bob,95000\n3,Charlie,85000";
        var tableWithHeaders = TabularParser.DetectAndParse(withHeadersCsv);
        Assert.NotNull(tableWithHeaders);
        Assert.True(tableWithHeaders.HasHeaders);
        Assert.Equal(new[] { "ID", "Name", "Salary" }, tableWithHeaders.Columns);
        Assert.Equal(3, tableWithHeaders.Rows.Count);
        Assert.Equal("1", tableWithHeaders.Rows[0][0]);

        // 2. CSV without headers (all rows are data rows, row 0 has numbers)
        string withoutHeadersCsv = "101,Alice,120000\n102,Bob,95000\n103,Charlie,85000";
        var tableWithoutHeaders = TabularParser.DetectAndParse(withoutHeadersCsv);
        Assert.NotNull(tableWithoutHeaders);
        Assert.False(tableWithoutHeaders.HasHeaders);
        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3" }, tableWithoutHeaders.Columns);
        Assert.Equal(3, tableWithoutHeaders.Rows.Count);
        Assert.Equal("101", tableWithoutHeaders.Rows[0][0]);
        Assert.Equal("Alice", tableWithoutHeaders.Rows[0][1]);

        // 3. TSV without headers (row 0 contains numeric order ID)
        string withoutHeadersTsv = "1001\tWidget A\t19.99\n1002\tWidget B\t29.99";
        var tsvWithoutHeaders = TabularParser.DetectAndParse(withoutHeadersTsv);
        Assert.NotNull(tsvWithoutHeaders);
        Assert.False(tsvWithoutHeaders.HasHeaders);
        Assert.Equal(2, tsvWithoutHeaders.Rows.Count);
        Assert.Equal("1001", tsvWithoutHeaders.Rows[0][0]);

        // 4. HTML table without <th> or <thead> and with numeric first row
        string htmlNoHeaders = "<table><tr><td>101</td><td>Raleigh</td></tr><tr><td>102</td><td>Durham</td></tr></table>";
        var htmlTable = TabularParser.DetectAndParse(htmlNoHeaders);
        Assert.NotNull(htmlTable);
        Assert.False(htmlTable.HasHeaders);
        Assert.Equal(new[] { "Column 1", "Column 2" }, htmlTable.Columns);
        Assert.Equal(2, htmlTable.Rows.Count);
        Assert.Equal("101", htmlTable.Rows[0][0]);

        // 5. HTML table with <th> tags (explicit headers)
        string htmlWithTh = "<table><tr><th>City ID</th><th>City Name</th></tr><tr><td>101</td><td>Raleigh</td></tr></table>";
        var htmlThTable = TabularParser.DetectAndParse(htmlWithTh);
        Assert.NotNull(htmlThTable);
        Assert.True(htmlThTable.HasHeaders);
        Assert.Equal(new[] { "City ID", "City Name" }, htmlThTable.Columns);
        Assert.Single(htmlThTable.Rows);
        Assert.Equal("101", htmlThTable.Rows[0][0]);
    }

    [Fact]
    public void TabularParser_ManualHeaderOverride_ForcesHeaderOrDataRow()
    {
        string csv = "101,Alice,Engineering\n102,Bob,Design";

        // When assumeHeader is explicitly true, row 0 becomes header
        var forcedHeader = TabularParser.Parse(csv, ',', assumeHeader: true);
        Assert.True(forcedHeader.HasHeaders);
        Assert.Equal(new[] { "101", "Alice", "Engineering" }, forcedHeader.Columns);
        Assert.Single(forcedHeader.Rows);
        Assert.Equal("102", forcedHeader.Rows[0][0]);

        // When assumeHeader is explicitly false, row 0 remains in data rows
        var forcedNoHeader = TabularParser.Parse(csv, ',', assumeHeader: false);
        Assert.False(forcedNoHeader.HasHeaders);
        Assert.Equal(new[] { "Column 1", "Column 2", "Column 3" }, forcedNoHeader.Columns);
        Assert.Equal(2, forcedNoHeader.Rows.Count);
        Assert.Equal("101", forcedNoHeader.Rows[0][0]);
        Assert.Equal("102", forcedNoHeader.Rows[1][0]);
    }

    [Fact]
    public void TabularConverter_HandlesTableWithoutHeaders()
    {
        var table = new TabularData
        {
            HasHeaders = false,
            Columns = new List<string> { "Column 1", "Column 2" },
            Rows = new List<List<string>>
            {
                new() { "101", "Alice" },
                new() { "102", "Bob" }
            }
        };

        // CSV without headers should not output the synthetic Column 1, Column 2 header row
        string csv = TabularConverter.ToCsv(table);
        Assert.DoesNotContain("Column 1", csv);
        Assert.Contains("101,Alice", csv);
        Assert.Contains("102,Bob", csv);

        // SQL IN from column 0
        string sqlIn = TabularConverter.ToSqlInClause(table, 0, quoteStrings: false);
        Assert.Equal("IN (101, 102)", sqlIn);

        // Markdown Table displays columns as headers
        string md = TabularConverter.ToMarkdownTable(table);
        Assert.Contains("Column 1", md);
        Assert.Contains("101", md);
    }

    [Fact]
    public void TabularParser_JsonArrayOfArrays_AutodetectsHeaders()
    {
        string jsonNoHeaders = "[[\"101\", \"Alice\"], [\"102\", \"Bob\"]]";
        var parsed = TabularParser.DetectAndParse(jsonNoHeaders);
        Assert.NotNull(parsed);
        Assert.False(parsed.HasHeaders);
        Assert.Equal(2, parsed.Rows.Count);
        Assert.Equal("101", parsed.Rows[0][0]);

        string jsonWithHeaders = "[[\"ID\", \"Name\"], [\"101\", \"Alice\"], [\"102\", \"Bob\"]]";
        var parsedHeaders = TabularParser.DetectAndParse(jsonWithHeaders);
        Assert.NotNull(parsedHeaders);
        Assert.True(parsedHeaders.HasHeaders);
        Assert.Equal(new[] { "ID", "Name" }, parsedHeaders.Columns);
        Assert.Equal(2, parsedHeaders.Rows.Count);
    }

    [Theory]
    [InlineData("Just plain text with multiple words on one line", false)]
    [InlineData("Item 1\nItem 2\nItem 3\nItem 4", false)]
    [InlineData("101\n102\n103\n104\n105", false)]
    [InlineData("key1=value1\nkey2=value2", false)]
    [InlineData("IN (1, 2, 3, 4)", false)]
    [InlineData("id,name,role\n1,Alice,Dev\n2,Bob,QA", true)]
    [InlineData("id\tname\trole\n1\tAlice\tDev", true)]
    [InlineData("<table><tr><th>A</th><th>B</th></tr><tr><td>1</td><td>2</td></tr></table>", true)]
    [InlineData("| Header 1 | Header 2 |\n|---|---|\n| Val 1 | Val 2 |", true)]
    public void TextAnalyzer_TabularDetection_IdentifiesTabularVsNonTabularText(string input, bool expectedIsTabular)
    {
        var result = TextAnalyzer.Analyze(input);
        Assert.Equal(expectedIsTabular, result.IsTabular);
    }
}
