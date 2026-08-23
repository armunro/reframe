using Reframe.Core.Structured;
using Reframe.Core.Structured.Transformers;
using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Case;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class StructuredTransformationsTests
{
    private const string SampleJson = """
    {
      "userId": 101,
      "firstName": "John",
      "lastName": "Doe",
      "contactInfo": {
        "emailAddress": "john.doe@example.com",
        "phoneNumber": "555-1234"
      },
      "roles": ["Admin", "Developer"],
      "emptyField": null,
      "blankNote": ""
    }
    """;

    private const string SampleYaml = """
    userId: 101
    firstName: John
    lastName: Doe
    contactInfo:
      emailAddress: john.doe@example.com
      phoneNumber: 555-1234
    roles:
      - Admin
      - Developer
    emptyField: null
    blankNote: ""
    """;

    private const string SampleXml = """
    <user id="101">
      <firstName>John</firstName>
      <lastName>Doe</lastName>
      <contactInfo>
        <emailAddress>john.doe@example.com</emailAddress>
        <phoneNumber>555-1234</phoneNumber>
      </contactInfo>
    </user>
    """;

    [Fact]
    public void ConvertKeysCase_SnakeCase_TransformsJsonKeys()
    {
        string result = StructuredTransformers.ConvertKeysCase(SampleJson, TextCasing.SnakeCase);
        Assert.Contains("\"user_id\"", result);
        Assert.Contains("\"first_name\"", result);
        Assert.Contains("\"last_name\"", result);
        Assert.Contains("\"contact_info\"", result);
        Assert.Contains("\"email_address\"", result);
        Assert.Contains("\"phone_number\"", result);
    }

    [Fact]
    public void ConvertKeysCase_CamelCase_TransformsYamlKeys()
    {
        string input = "First_Name: John\nLast_Name: Doe\nUser_Id: 101";
        string result = StructuredTransformers.ConvertKeysCase(input, TextCasing.CamelCase);
        Assert.Contains("firstName: John", result);
        Assert.Contains("lastName: Doe", result);
        Assert.Contains("userId: 101", result);
    }

    [Fact]
    public void ConvertKeysCase_KebabCase_TransformsJsonKeys()
    {
        string result = StructuredTransformers.ConvertKeysCase(SampleJson, TextCasing.KebabCase);
        Assert.Contains("\"first-name\"", result);
        Assert.Contains("\"contact-info\"", result);
        Assert.Contains("\"email-address\"", result);
    }

    [Fact]
    public void ConvertKeysCase_PascalCase_TransformsJsonKeys()
    {
        string result = StructuredTransformers.ConvertKeysCase(SampleJson, TextCasing.PascalCase);
        Assert.Contains("\"UserId\"", result);
        Assert.Contains("\"FirstName\"", result);
        Assert.Contains("\"ContactInfo\"", result);
        Assert.Contains("\"EmailAddress\"", result);
    }

    [Fact]
    public void PickKeys_KeepsOnlySpecifiedKeys()
    {
        string result = StructuredTransformers.PickKeys(SampleJson, "userId, firstName, emailAddress");
        Assert.Contains("\"userId\"", result);
        Assert.Contains("\"firstName\"", result);
        Assert.DoesNotContain("\"lastName\"", result);
        Assert.DoesNotContain("\"phoneNumber\"", result);
    }

    [Fact]
    public void OmitKeys_RemovesSpecifiedKeys()
    {
        string result = StructuredTransformers.OmitKeys(SampleJson, "emptyField, blankNote, contactInfo");
        Assert.Contains("\"userId\"", result);
        Assert.Contains("\"firstName\"", result);
        Assert.DoesNotContain("\"emptyField\"", result);
        Assert.DoesNotContain("\"blankNote\"", result);
        Assert.DoesNotContain("\"contactInfo\"", result);
    }

    [Fact]
    public void RemoveNullsAndEmpty_StripsNullAndEmptyFields()
    {
        string result = StructuredTransformers.RemoveNullsAndEmpty(SampleJson);
        Assert.Contains("\"userId\"", result);
        Assert.Contains("\"firstName\"", result);
        Assert.DoesNotContain("emptyField", result);
        Assert.DoesNotContain("blankNote", result);
    }

    [Fact]
    public void FlattenToFlatJson_ProducesDotNotatedObject()
    {
        string result = StructuredTransformers.FlattenToFlatJson(SampleJson);
        Assert.Contains("\"contactInfo.emailAddress\": \"john.doe@example.com\"", result);
        Assert.Contains("\"firstName\": \"John\"", result);
    }

    [Fact]
    public void QueryPath_RetrievesMatchingNodeValue()
    {
        string result = StructuredTransformers.QueryPath(SampleJson, "firstName");
        Assert.Equal("John", result.Trim());
    }

    [Fact]
    public void QueryPath_WithJsonPath_RetrievesSubtree()
    {
        string result = StructuredTransformers.QueryPath(SampleJson, "$.contactInfo.emailAddress");
        Assert.Equal("john.doe@example.com", result.Trim());
    }

    [Fact]
    public void ExtractValues_ExtractsAllLeafValues()
    {
        string result = StructuredTransformers.ExtractValues(SampleJson);
        Assert.Contains("101", result);
        Assert.Contains("John", result);
        Assert.Contains("Doe", result);
        Assert.Contains("john.doe@example.com", result);
    }

    [Fact]
    public void SortKeys_Descending_SortsKeysInReverseAlphabeticalOrder()
    {
        string result = StructuredTransformers.SortKeys(SampleJson, descending: true);
        int userIdx = result.IndexOf("\"userId\"");
        int firstIdx = result.IndexOf("\"firstName\"");
        Assert.True(userIdx < firstIdx);
    }

    [Fact]
    public void StructuredToCsv_ConvertsJsonArrayToCsv()
    {
        string jsonArray = """
        [
          {"id": 1, "name": "Alice", "role": "Eng"},
          {"id": 2, "name": "Bob", "role": "PM"}
        ]
        """;
        string csv = StructuredTransformers.ToCsv(jsonArray);
        Assert.Contains("id,name,role", csv);
        Assert.Contains("1,Alice,Eng", csv);
        Assert.Contains("2,Bob,PM", csv);
    }

    [Fact]
    public void StructuredToMarkdown_ConvertsJsonArrayToMarkdownTable()
    {
        string jsonArray = """
        [
          {"id": 1, "name": "Alice"},
          {"id": 2, "name": "Bob"}
        ]
        """;
        string md = StructuredTransformers.ToMarkdownTable(jsonArray);
        Assert.Contains("id", md);
        Assert.Contains("name", md);
        Assert.Contains("Alice", md);
        Assert.Contains("Bob", md);
        Assert.Contains("|", md);
    }

    [Fact]
    public void ToTypeScriptInterfaces_GeneratesValidInterfaces()
    {
        string ts = StructuredTransformers.ToTypeScriptInterfaces(SampleJson, "User");
        Assert.Contains("export interface User {", ts);
        Assert.Contains("userId: number;", ts);
        Assert.Contains("firstName: string;", ts);
        Assert.Contains("roles: string[];", ts);
        Assert.Contains("contactInfo: ContactInfo;", ts);
        Assert.Contains("export interface ContactInfo {", ts);
        Assert.Contains("emailAddress: string;", ts);
    }

    [Fact]
    public void ToCSharpClasses_GeneratesValidPocoClasses()
    {
        string cs = StructuredTransformers.ToCSharpClasses(SampleJson, "User");
        Assert.Contains("public class User", cs);
        Assert.Contains("public int UserId { get; set; }", cs);
        Assert.Contains("public string FirstName { get; set; } = string.Empty;", cs);
        Assert.Contains("public List<string> Roles { get; set; } = new();", cs);
        Assert.Contains("public ContactInfo ContactInfo { get; set; }", cs);
        Assert.Contains("public class ContactInfo", cs);
    }

    [Fact]
    public void ToJsonSchema_GeneratesValidJsonSchema()
    {
        string schema = StructuredTransformers.ToJsonSchema(SampleJson, "UserSchema");
        Assert.Contains("\"$schema\": \"http://json-schema.org/draft-07/schema#\"", schema);
        Assert.Contains("\"title\": \"UserSchema\"", schema);
        Assert.Contains("\"type\": \"object\"", schema);
        Assert.Contains("\"userId\": {", schema);
        Assert.Contains("\"type\": \"integer\"", schema);
        Assert.Contains("\"roles\": {", schema);
        Assert.Contains("\"type\": \"array\"", schema);
    }

    [Fact]
    public void ViewModel_StructuredTransformActions_WorkCorrectly()
    {
        var vm = new MainViewModel();
        vm.InputText = SampleJson;

        // 1. Camel to Snake Case
        vm.ActionCommand.Execute("StructuredSnakeCase");
        Assert.Contains("\"first_name\"", vm.OutputText);

        // 2. Pick Keys
        vm.StructuredFilterKeyList = "userId, firstName";
        vm.ActionCommand.Execute("PickStructuredKeys");
        Assert.Contains("\"userId\"", vm.OutputText);
        Assert.DoesNotContain("\"lastName\"", vm.OutputText);

        // 3. Query Path
        vm.StructuredQueryPath = "firstName";
        vm.ActionCommand.Execute("QueryStructuredPath");
        Assert.Equal("John", vm.OutputText.Trim());

        // 4. Generate TypeScript Interface
        vm.ActionCommand.Execute("ToTypeScriptInterfaces");
        Assert.Contains("export interface Root {", vm.OutputText);

        // 5. Generate JSON Schema
        vm.ActionCommand.Execute("ToJsonSchema");
        Assert.Contains("\"$schema\": \"http://json-schema.org/draft-07/schema#\"", vm.OutputText);
    }
}
