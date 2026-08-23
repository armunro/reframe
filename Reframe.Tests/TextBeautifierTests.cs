using Reframe.Core.Transformers;
using Reframe.Core.Transformers.Formatting;
using Xunit;

namespace Reframe.Tests;

public class TextBeautifierTests
{
    [Fact]
    public void Beautify_MinifiedJsonObject_BeautifiesWithIndentation()
    {
        string minifiedJson = "{\"name\":\"John\",\"age\":30,\"cars\":[\"Ford\",\"BMW\"]}";
        string beautified = TextBeautifier.Beautify(minifiedJson);

        Assert.Contains("\n", beautified);
        Assert.Contains("  \"name\": \"John\"", beautified);
        Assert.Contains("  \"age\": 30", beautified);
        Assert.Contains("    \"Ford\"", beautified);
    }

    [Fact]
    public void Beautify_MinifiedJsonArray_BeautifiesWithIndentation()
    {
        string minifiedArray = "[{\"id\":1,\"value\":\"A\"},{\"id\":2,\"value\":\"B\"}]";
        string beautified = TextBeautifier.Beautify(minifiedArray);

        Assert.Contains("\n", beautified);
        Assert.Contains("  {", beautified);
        Assert.Contains("    \"id\": 1", beautified);
    }

    [Fact]
    public void Beautify_XmlFragment_BeautifiesWithIndentation()
    {
        string rawXml = "<root><user id=\"1\"><name>Alice</name></user><user id=\"2\"><name>Bob</name></user></root>";
        string beautified = TextBeautifier.Beautify(rawXml);

        Assert.Contains("\r\n", beautified);
        Assert.Contains("<root>", beautified);
        Assert.Contains("  <user id=\"1\">", beautified);
        Assert.Contains("    <name>Alice</name>", beautified);
    }

    [Fact]
    public void Beautify_HtmlTable_BeautifiesXmlCompatibleTable()
    {
        string rawTable = "<table><thead><tr><th>Name</th><th>Age</th></tr></thead><tbody><tr><td>Alice</td><td>30</td></tr></tbody></table>";
        string beautified = TextBeautifier.Beautify(rawTable);

        Assert.Contains("<table>", beautified);
        Assert.Contains("  <thead>", beautified);
        Assert.Contains("    <tr>", beautified);
        Assert.Contains("      <th>Name</th>", beautified);
    }

    [Theory]
    [InlineData("Just plain text with no markup")]
    [InlineData("1,2,3,4,5")]
    [InlineData("123456")]
    [InlineData("SELECT * FROM Users WHERE Id = 1")]
    [InlineData("a < b and c > d")]
    [InlineData("{ invalid json: 123 ")]
    [InlineData("<broken xml")]
    public void Beautify_NonBeautifiableOrInvalidInput_ReturnsOriginal(string input)
    {
        string result = TextBeautifier.Beautify(input);
        Assert.Equal(input, result);
    }

    [Theory]
    [InlineData("{\"a\":1}", true)]
    [InlineData("[1, 2, 3]", true)]
    [InlineData("<root><item/></root>", true)]
    [InlineData("plain text", false)]
    [InlineData("123", false)]
    [InlineData("{ invalid json", false)]
    [InlineData("< unclosed", false)]
    public void CanBeautify_DetectsBeautifiableFormatsCorrectly(string input, bool expected)
    {
        Assert.Equal(expected, TextBeautifier.CanBeautify(input));
    }

    [Fact]
    public void BeautifyJson_HandlesNullOrWhitespace()
    {
        Assert.Equal(string.Empty, TextBeautifier.BeautifyJson(null));
        Assert.Equal(string.Empty, TextBeautifier.BeautifyJson("   "));
    }

    [Fact]
    public void BeautifyXml_HandlesNullOrWhitespace()
    {
        Assert.Equal(string.Empty, TextBeautifier.BeautifyXml(null));
        Assert.Equal(string.Empty, TextBeautifier.BeautifyXml("   "));
    }
}
