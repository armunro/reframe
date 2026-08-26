using Reframe.Core.Analysis.Analyzers;
using Reframe.Core.Analysis.Models;
using Reframe.Highlighting;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class SqlSyntaxHighlightingTests
{
    [Theory]
    [InlineData("SELECT * FROM Users WHERE Id = 1;")]
    [InlineData("select id, name, email from accounts where active = 1;")]
    [InlineData("SELECT 1;")]
    [InlineData("SELECT\n  id,\n  name\nFROM users;")]
    [InlineData("INSERT INTO [Users] ([Id], [Name]) VALUES (1, 'Alice');")]
    [InlineData("INSERT INTO Users (Id, Name) VALUES (1, 'Alice'), (2, 'Bob');")]
    [InlineData("UPDATE Users SET Status = 'Active' WHERE Id = 1;")]
    [InlineData("DELETE FROM Users WHERE Id = 1;")]
    [InlineData("CREATE TABLE Users (\n  Id INT PRIMARY KEY,\n  Name VARCHAR(50)\n);")]
    [InlineData("ALTER TABLE Users ADD Email VARCHAR(100);")]
    [InlineData("DROP TABLE OldUsers;")]
    [InlineData("TRUNCATE TABLE TempLogs;")]
    [InlineData("MERGE INTO Target AS T USING Source AS S ON T.Id = S.Id WHEN MATCHED THEN UPDATE SET T.Val = S.Val;")]
    [InlineData("WITH RankedUsers AS (SELECT *, ROW_NUMBER() OVER (ORDER BY Id) as rn FROM Users) SELECT * FROM RankedUsers;")]
    [InlineData("USE [MyDatabase];")]
    [InlineData("EXEC sp_who2;")]
    [InlineData("DECLARE @UserId INT = 1; SELECT * FROM Users WHERE Id = @UserId;")]
    public void InputEditor_SqlStatements_AutoSelectsSqlSyntax(string sql)
    {
        var vm = new MainViewModel();
        vm.InputText = sql;

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal(DetectedFormat.Sql, vm.Analysis.Format);
        Assert.True(vm.IsCodeTabHighlighted);
        Assert.Equal(3, vm.SelectedSidebarTabIndex); // Code tab
    }

    [Theory]
    [InlineData("-- Query users\nSELECT * FROM Users;")]
    [InlineData("/* Multi-line\n   comment */\nSELECT * FROM Users;")]
    [InlineData("-- Comment 1\n-- Comment 2\nINSERT INTO Users (Id) VALUES (1);")]
    public void InputEditor_SqlWithComments_AutoSelectsSqlSyntax(string sql)
    {
        var vm = new MainViewModel();
        vm.InputText = sql;

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal(DetectedFormat.Sql, vm.Analysis.Format);
        Assert.True(vm.IsCodeTabHighlighted);
    }

    [Fact]
    public void InputEditor_MultipleSqlInsertStatements_AutoSelectsSqlSyntax_NotCsv()
    {
        var vm = new MainViewModel();
        vm.InputText = "INSERT INTO [Users] ([Id], [Name], [Score]) VALUES (1, 'Alice', 95.5);\nINSERT INTO [Users] ([Id], [Name], [Score]) VALUES (2, 'Bob', 80);";

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal(DetectedFormat.Sql, vm.Analysis.Format);
        Assert.False(vm.Analysis.IsTabular);
        Assert.False(vm.HasTabularData);
    }

    [Theory]
    [InlineData("IN (1001, 1002, 1003)")]
    [InlineData("WHERE id IN (1001, 1002, 1003)")]
    [InlineData("IN (\n    1001,\n    1002,\n    1003\n)")]
    [InlineData("WHERE [user_id] IN ('USR-1', 'USR-2')")]
    public void InputEditor_SqlInClause_AutoSelectsSqlSyntax(string sqlIn)
    {
        var vm = new MainViewModel();
        vm.InputText = sqlIn;

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal(DetectedFormat.SqlInClause, vm.Analysis.Format);
        Assert.True(vm.IsCodeTabHighlighted);
    }

    [Fact]
    public void OutputEditor_ToSqlInsertsAction_AutoSelectsSqlSyntax()
    {
        var vm = new MainViewModel();
        vm.InputText = "Id,Name,Score\n1,Alice,95.5\n2,Bob,80";
        vm.SqlTableName = "Users";

        vm.ActionCommand.Execute("ToSqlInserts");

        Assert.Contains("INSERT INTO [Users]", vm.OutputText);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void OutputEditor_SqlInAction_AutoSelectsSqlSyntax()
    {
        var vm = new MainViewModel();
        vm.InputText = "101\n102\n103";

        vm.ActionCommand.Execute("SqlIn");

        Assert.Equal("IN (101, 102, 103)", vm.OutputText);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void OutputEditor_SqlInMultiLineAction_AutoSelectsSqlSyntax()
    {
        var vm = new MainViewModel();
        vm.InputText = "101\n102\n103";

        vm.ActionCommand.Execute("SqlInMultiLine");

        Assert.Contains("IN (", vm.OutputText);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void OutputEditor_ExtractSelectedToSqlInAction_AutoSelectsSqlSyntax()
    {
        var vm = new MainViewModel();
        vm.InputText = "Id,Name\n101,Alice\n102,Bob";

        vm.ActionCommand.Execute("ExtractSelectedToSqlIn");

        Assert.Contains("IN (", vm.OutputText);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void OutputEditor_DirectSqlText_AutoSelectsSqlSyntax()
    {
        var vm = new MainViewModel();
        vm.OutputText = "SELECT * FROM Products WHERE Stock > 0;";

        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Fact]
    public void SyntaxHighlighting_ManualSelectionOverridesAuto()
    {
        var vm = new MainViewModel();
        vm.InputText = "SELECT * FROM Users;";
        vm.OutputText = "SELECT * FROM Users;";

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);

        // Override to Plain Text
        vm.SelectedInputSyntax = "Plain Text";
        vm.SelectedOutputSyntax = "Plain Text";

        Assert.Equal("Plain Text", vm.EffectiveInputSyntax);
        Assert.Equal("Plain Text", vm.EffectiveOutputSyntax);

        // Reset to Auto
        vm.SelectedInputSyntax = "Auto";
        vm.SelectedOutputSyntax = "Auto";

        Assert.Equal("SQL", vm.EffectiveInputSyntax);
        Assert.Equal("SQL", vm.EffectiveOutputSyntax);
    }

    [Theory]
    [InlineData("Selection of favorite items in store")]
    [InlineData("Update on project status meeting")]
    [InlineData("Delete this unnecessary line")]
    [InlineData("Id,Selection,Name\n1,Yes,Alice\n2,No,Bob")]
    [InlineData("{\"select\": 1, \"update\": false}")]
    public void IsSql_NegativeCases_ReturnsFalse(string nonSql)
    {
        Assert.False(DefaultTextAnalyzer.IsSql(nonSql));
    }

    [Fact]
    public void PropertyChanged_FiresForEffectiveSyntax()
    {
        var vm = new MainViewModel();
        bool inputSyntaxChanged = false;
        bool outputSyntaxChanged = false;

        vm.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.EffectiveInputSyntax)) inputSyntaxChanged = true;
            if (e.PropertyName == nameof(MainViewModel.EffectiveOutputSyntax)) outputSyntaxChanged = true;
        };

        vm.InputText = "SELECT * FROM Users;";
        Assert.True(inputSyntaxChanged);

        vm.OutputText = "SELECT * FROM Orders;";
        Assert.True(outputSyntaxChanged);
    }
}
