using System;
using System.Data;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Reframe.Core.Actions;
using Reframe.Core.RegexLab;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class RegexLabTests
{
    private readonly RegexLabEngine _engine = new();

    [Fact]
    public void RegexLabEngine_EvaluatesNamedAndNumberedCaptureGroups()
    {
        string pattern = @"\b(?<user>[a-zA-Z0-9._%+-]+)@(?<domain>[a-zA-Z0-9.-]+\.[a-zA-Z]{2,})\b";
        string input = "Contact dev@example.com and team-lead+alerts@sub.corp.org for info.";

        var result = _engine.Evaluate(input, pattern, RegexOptions.IgnoreCase);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(2, result.TotalMatches);
        Assert.Equal(4, result.TotalGroups); // 2 groups * 2 matches
        Assert.Equal(2, result.Matches.Count);

        // Match 1
        var match1 = result.Matches[0];
        Assert.Equal(1, match1.MatchNumber);
        Assert.Equal("dev@example.com", match1.Value);
        Assert.Equal(8, match1.Index);
        Assert.Equal(15, match1.Length);

        var userGroup1 = match1.GetGroup("user");
        Assert.NotNull(userGroup1);
        Assert.Equal("dev", userGroup1.Value);
        Assert.True(userGroup1.IsNamed);

        var domainGroup1 = match1.GetGroup("domain");
        Assert.NotNull(domainGroup1);
        Assert.Equal("example.com", domainGroup1.Value);

        // Match 2
        var match2 = result.Matches[1];
        Assert.Equal(2, match2.MatchNumber);
        Assert.Equal("team-lead+alerts@sub.corp.org", match2.Value);

        var userGroup2 = match2.GetGroup("user");
        Assert.NotNull(userGroup2);
        Assert.Equal("team-lead+alerts", userGroup2.Value);

        var domainGroup2 = match2.GetGroup("domain");
        Assert.NotNull(domainGroup2);
        Assert.Equal("sub.corp.org", domainGroup2.Value);
    }

    [Fact]
    public void RegexLabEngine_BuildsGroupExtractionDataTable()
    {
        string pattern = @"(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})";
        string input = "Dates: 2026-08-26 and 2024-12-31";

        var result = _engine.Evaluate(input, pattern);

        Assert.True(result.IsValid);
        Assert.NotNull(result.GroupTable);
        var table = result.GroupTable;

        Assert.Contains("Match #", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("Index", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("Length", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("Full Match", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("$year", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("$month", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));
        Assert.Contains("$day", table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

        Assert.Equal(2, table.Rows.Count);
        Assert.Equal(1, table.Rows[0]["Match #"]);
        Assert.Equal("2026-08-26", table.Rows[0]["Full Match"]);
        Assert.Equal("2026", table.Rows[0]["$year"]);
        Assert.Equal("08", table.Rows[0]["$month"]);
        Assert.Equal("26", table.Rows[0]["$day"]);

        Assert.Equal(2, table.Rows[1]["Match #"]);
        Assert.Equal("2024-12-31", table.Rows[1]["Full Match"]);
        Assert.Equal("2024", table.Rows[1]["$year"]);
        Assert.Equal("12", table.Rows[1]["$month"]);
        Assert.Equal("31", table.Rows[1]["$day"]);
    }

    [Fact]
    public void RegexLabEngine_ExtractsMatchesAsDelimitedAndJson()
    {
        string pattern = @"\b(?<key>[a-z]+)=(?<val>\d+)\b";
        string input = "foo=10 bar=20 baz=30";

        var result = _engine.Evaluate(input, pattern);

        // 1. Matches as lines
        string matches = _engine.ExtractMatches(result, "\n");
        Assert.Equal("foo=10\nbar=20\nbaz=30", matches);

        // 2. Groups as TSV table
        string tsv = _engine.ExtractGroupsAsDelimited(result, "\t", includeHeaders: true);
        Assert.Contains("Match #\tIndex\tLength\tFull Match\t$key\t$val", tsv);
        Assert.Contains("1\t0\t6\tfoo=10\tfoo\t10", tsv);
        Assert.Contains("2\t7\t6\tbar=20\tbar\t20", tsv);
        Assert.Contains("3\t14\t6\tbaz=30\tbaz\t30", tsv);

        // 3. Groups as JSON
        string json = _engine.ExtractGroupsAsJson(result, indented: true);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        Assert.Equal(JsonValueKind.Array, root.ValueKind);
        Assert.Equal(3, root.GetArrayLength());

        var first = root[0];
        Assert.Equal(1, first.GetProperty("matchNumber").GetInt32());
        Assert.Equal("foo=10", first.GetProperty("match").GetString());
        Assert.Equal("foo", first.GetProperty("groups").GetProperty("key").GetString());
        Assert.Equal("10", first.GetProperty("groups").GetProperty("val").GetString());
    }

    [Fact]
    public void RegexLabEngine_HandlesInvalidPatternSyntax_Gracefully()
    {
        string invalidPattern = @"\b(?<invalid[0-9]+";
        string input = "Test text";

        var result = _engine.Evaluate(input, invalidPattern);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.NotEmpty(result.ErrorMessage);
        Assert.Empty(result.Matches);
    }

    [Fact]
    public void RegexLabEngine_HandlesEmptyPatternAndInput()
    {
        var result1 = _engine.Evaluate("", "");
        Assert.True(result1.IsValid);
        Assert.Empty(result1.Matches);

        var result2 = _engine.Evaluate("Some text", "");
        Assert.True(result2.IsValid);
        Assert.Empty(result2.Matches);

        var result3 = _engine.Evaluate("", @"\d+");
        Assert.True(result3.IsValid);
        Assert.Empty(result3.Matches);
    }

    [Fact]
    public void RegexLabEngine_OptionsToggles_AffectResults()
    {
        string pattern = "^hello$";
        string input = "Hello\nhello\nHELLO";

        // Without multiline/ignorecase
        var res1 = _engine.Evaluate(input, pattern, RegexOptions.None);
        Assert.Equal(0, res1.TotalMatches);

        // With multiline
        var res2 = _engine.Evaluate(input, pattern, RegexOptions.Multiline);
        Assert.Equal(1, res2.TotalMatches);
        Assert.Equal("hello", res2.Matches[0].Value);

        // With multiline + ignorecase
        var res3 = _engine.Evaluate(input, pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        Assert.Equal(3, res3.TotalMatches);
    }

    [Fact]
    public void RegexLibraryCatalog_AllPresetsCompileAndMatchSamples()
    {
        var presets = RegexLibraryCatalog.Presets;
        Assert.NotEmpty(presets);
        Assert.True(presets.Count >= 12, $"Expected at least 12 presets, found {presets.Count}");

        foreach (var preset in presets)
        {
            Assert.False(string.IsNullOrWhiteSpace(preset.Id), $"Preset {preset.Name} has empty ID");
            Assert.False(string.IsNullOrWhiteSpace(preset.Name), $"Preset {preset.Id} has empty Name");
            Assert.False(string.IsNullOrWhiteSpace(preset.Pattern), $"Preset {preset.Name} has empty Pattern");
            Assert.False(string.IsNullOrWhiteSpace(preset.SampleText), $"Preset {preset.Name} has empty SampleText");

            // Verify regex compiles and executes on sample
            var result = _engine.Evaluate(preset.SampleText, preset.Pattern, preset.DefaultOptions);
            Assert.True(result.IsValid, $"Preset '{preset.Name}' failed regex compilation: {result.ErrorMessage}");
            Assert.True(result.TotalMatches > 0, $"Preset '{preset.Name}' found 0 matches on its own SampleText: {preset.SampleText}");
        }
    }

    [Fact]
    public void RegexLibraryCatalog_TestsSpecificPresets_Explicitly()
    {
        // 1. ISO 8601 Date
        var iso = RegexLibraryCatalog.FindById("iso-8601-date");
        Assert.NotNull(iso);
        var isoResult = _engine.Evaluate("Event at 2026-08-26T18:46:00.000Z and end 2026-08-27", iso.Pattern, iso.DefaultOptions);
        Assert.Equal(2, isoResult.TotalMatches);
        Assert.Equal("2026", isoResult.Matches[0].GetGroup("year")?.Value);
        Assert.Equal("08", isoResult.Matches[0].GetGroup("month")?.Value);
        Assert.Equal("26", isoResult.Matches[0].GetGroup("day")?.Value);

        // 2. Email Address
        var email = RegexLibraryCatalog.FindById("email-address");
        Assert.NotNull(email);
        var emailResult = _engine.Evaluate("Send to dev@reframe.io or support@jetbrains.com", email.Pattern, email.DefaultOptions);
        Assert.Equal(2, emailResult.TotalMatches);
        Assert.Equal("dev", emailResult.Matches[0].GetGroup("user")?.Value);
        Assert.Equal("reframe.io", emailResult.Matches[0].GetGroup("domain")?.Value);

        // 3. SemVer
        var semver = RegexLibraryCatalog.FindById("semver");
        Assert.NotNull(semver);
        var semverResult = _engine.Evaluate("Releases: v1.2.3 and 2.0.0-beta.1+build123", semver.Pattern, semver.DefaultOptions);
        Assert.Equal(2, semverResult.TotalMatches);
        Assert.Equal("1", semverResult.Matches[0].GetGroup("major")?.Value);
        Assert.Equal("2", semverResult.Matches[0].GetGroup("minor")?.Value);
        Assert.Equal("3", semverResult.Matches[0].GetGroup("patch")?.Value);
        Assert.Equal("beta.1", semverResult.Matches[1].GetGroup("prerelease")?.Value);

        // 4. UUID / GUID
        var uuid = RegexLibraryCatalog.FindById("uuid-guid");
        Assert.NotNull(uuid);
        var uuidResult = _engine.Evaluate("Keys: e029b8b2-3d77-4b68-b7a4-098e94be7882 and 550e8400-e29b-41d4-a716-446655440000", uuid.Pattern, uuid.DefaultOptions);
        Assert.Equal(2, uuidResult.TotalMatches);
        Assert.Equal("e029b8b2-3d77-4b68-b7a4-098e94be7882", uuidResult.Matches[0].GetGroup("guid")?.Value);

        // 5. JWT Token
        var jwt = RegexLibraryCatalog.FindById("jwt-token");
        Assert.NotNull(jwt);
        var jwtResult = _engine.Evaluate(jwt.SampleText, jwt.Pattern, jwt.DefaultOptions);
        Assert.Equal(1, jwtResult.TotalMatches);
        Assert.StartsWith("eyJ", jwtResult.Matches[0].GetGroup("header")?.Value ?? "");
        Assert.StartsWith("eyJ", jwtResult.Matches[0].GetGroup("payload")?.Value ?? "");

        // 6. IPv4 Address
        var ipv4 = RegexLibraryCatalog.FindById("ipv4-address");
        Assert.NotNull(ipv4);
        var ipv4Result = _engine.Evaluate("IP: 192.168.1.1 and 10.0.0.254", ipv4.Pattern, ipv4.DefaultOptions);
        Assert.Equal(2, ipv4Result.TotalMatches);
        Assert.Equal("192", ipv4Result.Matches[0].GetGroup("octet1")?.Value);
        Assert.Equal("168", ipv4Result.Matches[0].GetGroup("octet2")?.Value);
        Assert.Equal("1", ipv4Result.Matches[0].GetGroup("octet3")?.Value);
        Assert.Equal("1", ipv4Result.Matches[0].GetGroup("octet4")?.Value);

        // 7. IPv6 Address
        var ipv6 = RegexLibraryCatalog.FindById("ipv6-address");
        Assert.NotNull(ipv6);
        var ipv6Result = _engine.Evaluate(ipv6.SampleText, ipv6.Pattern, ipv6.DefaultOptions);
        Assert.True(ipv6Result.TotalMatches >= 2);

        // 8. Connection String
        var conn = RegexLibraryCatalog.FindById("connection-string");
        Assert.NotNull(conn);
        var connResult = _engine.Evaluate(conn.SampleText, conn.Pattern, conn.DefaultOptions);
        Assert.True(connResult.TotalMatches >= 4);
        Assert.Equal("Server", connResult.Matches[0].GetGroup("key")?.Value);

        // 9. URL
        var url = RegexLibraryCatalog.FindById("url-http-https");
        Assert.NotNull(url);
        var urlResult = _engine.Evaluate(url.SampleText, url.Pattern, url.DefaultOptions);
        Assert.True(urlResult.TotalMatches >= 2);
        Assert.Equal("https", urlResult.Matches[0].GetGroup("protocol")?.Value);
        Assert.Equal("api.example.com", urlResult.Matches[0].GetGroup("domain")?.Value);

        // 10. Hex Color
        var hex = RegexLibraryCatalog.FindById("hex-color");
        Assert.NotNull(hex);
        var hexResult = _engine.Evaluate(hex.SampleText, hex.Pattern, hex.DefaultOptions);
        Assert.True(hexResult.TotalMatches >= 4);

        // 11. Phone Number
        var phone = RegexLibraryCatalog.FindById("phone-number");
        Assert.NotNull(phone);
        var phoneResult = _engine.Evaluate(phone.SampleText, phone.Pattern, phone.DefaultOptions);
        Assert.True(phoneResult.TotalMatches >= 2);

        // 12. Markdown Links
        var md = RegexLibraryCatalog.FindById("markdown-links");
        Assert.NotNull(md);
        var mdResult = _engine.Evaluate(md.SampleText, md.Pattern, md.DefaultOptions);
        Assert.Equal(2, mdResult.TotalMatches);
        Assert.Equal("JetBrains", mdResult.Matches[0].GetGroup("text")?.Value);
        Assert.Equal("https://jetbrains.com", mdResult.Matches[0].GetGroup("url")?.Value);
    }

    [Fact]
    public void MainViewModel_RegexLab_LivePatternTestingAndExtractionCommands()
    {
        var vm = new MainViewModel();

        // 1. Set input text
        vm.InputText = "Server=db01;Database=Inventory;Port=5432;\nServer=db02;Database=Customers;Port=5432;";
        vm.RegexLabPattern = @"(?<key>[a-zA-Z]+)=(?<val>[a-zA-Z0-9]+);";

        Assert.True(vm.HasRegexMatches);
        Assert.Equal(6, vm.RegexMatchCount);
        Assert.False(vm.RegexHasError);
        Assert.Contains("6 matches", vm.RegexStatusMessage);
        Assert.NotNull(vm.RegexGroupDataTable);
        Assert.Equal(6, vm.RegexGroupDataTable.Rows.Count);

        // 2. Extract Matches Command
        vm.ExtractRegexMatchesCommand.Execute(null);
        Assert.Contains("Server=db01;", vm.OutputText);
        Assert.Contains("Database=Inventory;", vm.OutputText);
        Assert.Contains("Port=5432;", vm.OutputText);

        // 3. Extract TSV Table Command
        vm.ExtractRegexGroupsTableCommand.Execute(null);
        Assert.Contains("$key\t$val", vm.OutputText);
        Assert.Contains("Server\tdb01", vm.OutputText);

        // 4. Extract JSON Command
        vm.ExtractRegexGroupsJsonCommand.Execute(null);
        Assert.StartsWith("[", vm.OutputText.Trim());
        Assert.Contains("\"key\": \"Server\"", vm.OutputText);
        Assert.Contains("\"val\": \"db01\"", vm.OutputText);
    }

    [Fact]
    public void MainViewModel_RegexLab_PresetSelectionAndSampleLoading()
    {
        var vm = new MainViewModel();
        var emailPreset = RegexLibraryCatalog.FindById("email-address");
        Assert.NotNull(emailPreset);

        vm.ApplyRegexPreset(emailPreset, loadSample: true);

        Assert.Equal(emailPreset.Pattern, vm.RegexLabPattern);
        Assert.Equal(emailPreset.SampleText, vm.InputText);
        Assert.True(vm.HasRegexMatches);
        Assert.True(vm.RegexMatchCount >= 2);
    }

    [Fact]
    public void MainViewModel_ActionRegistry_ContainsRegexActionsAndPresets()
    {
        var regexLabAction = ActionRegistry.AllActions.FirstOrDefault(a => a.Id == "OpenRegexLab");
        Assert.NotNull(regexLabAction);
        Assert.Equal("Regex Lab", regexLabAction.Category);

        var matches = ActionRegistry.Search("regex email");
        Assert.NotEmpty(matches);
        Assert.Contains(matches, a => a.Id == "RegexPreset:email-address" || a.Id == "ExtractEmails");

        var vm = new MainViewModel();
        var emailAction = ActionRegistry.AllActions.First(a => a.Id == "RegexPreset:email-address");

        vm.ExecuteActionItem(emailAction);
        Assert.Equal(3, vm.SelectedCenterTabIndex); // Switched to Regex Lab Tab
        Assert.Equal(emailAction.Keywords.First(), "email regex");
    }
}
