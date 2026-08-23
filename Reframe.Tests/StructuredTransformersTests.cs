using Reframe.Core.Structured;
using Reframe.Core.Structured.Transformers;
using Xunit;

namespace Reframe.Tests;

public class StructuredTransformersTests
{
    [Fact]
    public void XmlToJson_ConvertsXmlWithAttributesAndElementsToJson()
    {
        string xml = @"<catalog store=""Main"">
  <item id=""1"">
    <name>Product A</name>
    <price>19.99</price>
    <active>true</active>
  </item>
  <item id=""2"">
    <name>Product B</name>
    <price>29.50</price>
    <active>false</active>
  </item>
</catalog>";

        string json = StructuredTransformers.XmlToJson(xml, indented: true);

        Assert.Contains("\"catalog\":", json);
        Assert.Contains("\"@store\": \"Main\"", json);
        Assert.Contains("\"item\": [", json);
        Assert.Contains("\"@id\": 1", json);
        Assert.Contains("\"name\": \"Product A\"", json);
        Assert.Contains("\"price\": 19.99", json);
        Assert.Contains("\"active\": true", json);
    }

    [Fact]
    public void JsonToXml_ConvertsJsonWithArraysAndAttributesToXml()
    {
        string json = @"
{
  ""@version"": ""1.0"",
  ""title"": ""Reframe"",
  ""items"": [
    { ""id"": 1, ""name"": ""LineOps"" },
    { ""id"": 2, ""name"": ""Structured"" }
  ]
}";

        string xml = StructuredTransformers.JsonToXml(json, rootElementName: "app", indented: true);

        Assert.Contains("<app version=\"1.0\">", xml);
        Assert.Contains("<title>Reframe</title>", xml);
        Assert.Contains("<items>", xml);
        Assert.Contains("<id>1</id>", xml);
        Assert.Contains("<name>LineOps</name>", xml);
    }

    [Fact]
    public void XmlToYaml_ConvertsXmlToYaml()
    {
        string xml = @"<server name=""web-prod"">
  <port>443</port>
  <ssl>true</ssl>
</server>";

        string yaml = StructuredTransformers.XmlToYaml(xml);

        Assert.Contains("server:", yaml);
        Assert.Contains("@name", yaml);
        Assert.Contains("port: 443", yaml);
        Assert.Contains("ssl: true", yaml);
    }

    [Fact]
    public void YamlToXml_ConvertsYamlToXml()
    {
        string yaml = @"
service: auth
port: 5000
enabled: true
";

        string xml = StructuredTransformers.YamlToXml(yaml, rootElementName: "config", indented: true);

        Assert.Contains("<config>", xml);
        Assert.Contains("<service>auth</service>", xml);
        Assert.Contains("<port>5000</port>", xml);
        Assert.Contains("<enabled>true</enabled>", xml);
    }

    [Fact]
    public void MinifyJson_RemovesWhitespaceAndNewlines()
    {
        string json = @"
{
  ""id"": 101,
  ""name"": ""Alice"",
  ""roles"": [ ""admin"", ""user"" ]
}";

        string minified = StructuredTransformers.MinifyJson(json);

        Assert.DoesNotContain("\n", minified);
        Assert.DoesNotContain("\r", minified);
        Assert.Equal("{\"id\":101,\"name\":\"Alice\",\"roles\":[\"admin\",\"user\"]}", minified);
    }

    [Fact]
    public void MinifyXml_RemovesWhitespaceAndIndents()
    {
        string xml = @"
<root>
  <item id=""1"">
    <value>Test</value>
  </item>
</root>";

        string minified = StructuredTransformers.MinifyXml(xml);

        Assert.DoesNotContain("\r\n", minified);
        Assert.Contains("<root><item id=\"1\"><value>Test</value></item></root>", minified);
    }

    [Fact]
    public void Flatten_ConvertsNestedStructureToDotNotation()
    {
        string json = @"
{
  ""user"": {
    ""name"": ""Alice"",
    ""address"": {
      ""city"": ""Seattle"",
      ""zip"": 98101
    },
    ""roles"": [""admin"", ""editor""]
  }
}";

        string flat = StructuredTransformers.Flatten(json);

        Assert.Contains("user.name = \"Alice\"", flat);
        Assert.Contains("user.address.city = \"Seattle\"", flat);
        Assert.Contains("user.address.zip = 98101", flat);
        Assert.Contains("user.roles[0] = \"admin\"", flat);
        Assert.Contains("user.roles[1] = \"editor\"", flat);
    }

    [Fact]
    public void Unflatten_ConvertsFlatDotNotationBackToJson()
    {
        string flat = @"
user.name = Alice
user.address.city = Seattle
user.address.zip = 98101
user.roles[0] = admin
user.roles[1] = editor
";

        string json = StructuredTransformers.Unflatten(flat, format: "JSON");

        Assert.Contains("\"name\": \"Alice\"", json);
        Assert.Contains("\"city\": \"Seattle\"", json);
        Assert.Contains("\"zip\": 98101", json);
        Assert.Contains("\"admin\"", json);
        Assert.Contains("\"editor\"", json);
    }

    [Fact]
    public void ExtractPaths_ReturnsAllDistinctPaths()
    {
        string json = @"
{
  ""app"": {
    ""name"": ""Reframe"",
    ""version"": 1.0
  }
}";

        string paths = StructuredTransformers.ExtractPaths(json);

        Assert.Contains("$.app", paths);
        Assert.Contains("$.app.name", paths);
        Assert.Contains("$.app.version", paths);
    }

    [Fact]
    public void ExtractKeys_ReturnsAllLeafKeys()
    {
        string json = @"
{
  ""store"": {
    ""title"": ""Bookstore"",
    ""location"": {
      ""city"": ""Seattle"",
      ""country"": ""USA""
    }
  }
}";

        string keys = StructuredTransformers.ExtractKeys(json);

        Assert.Contains("title", keys);
        Assert.Contains("city", keys);
        Assert.Contains("country", keys);
    }

    [Fact]
    public void SortKeys_SortsPropertiesAlphabetically()
    {
        string json = "{\"z\": 1, \"a\": 2, \"m\": {\"y\": 10, \"b\": 20}}";

        string sorted = StructuredTransformers.SortKeys(json);

        int idxA = sorted.IndexOf("\"a\":");
        int idxM = sorted.IndexOf("\"m\":");
        int idxZ = sorted.IndexOf("\"z\":");
        int idxB = sorted.IndexOf("\"b\":");
        int idxY = sorted.IndexOf("\"y\":");

        Assert.True(idxA < idxM);
        Assert.True(idxM < idxZ);
        Assert.True(idxB < idxY);
    }
}
