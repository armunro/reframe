using TextForge.Core.Structured;
using Xunit;

namespace TextForge.Tests;

public class StructuredDataParserTests
{
    [Fact]
    public void Parse_EmptyOrNull_ReturnsEmptyResult()
    {
        var resultNull = StructuredDataParser.Parse(null);
        var resultEmpty = StructuredDataParser.Parse("   ");

        Assert.False(resultNull.Success);
        Assert.Equal("Empty", resultNull.Format);
        Assert.False(resultEmpty.Success);
        Assert.Equal("Empty", resultEmpty.Format);
    }

    [Fact]
    public void Parse_ValidJson_CreatesHierarchicalTree()
    {
        string json = @"
{
  ""id"": 101,
  ""name"": ""TextForge"",
  ""isAwesome"": true,
  ""tags"": [""utility"", ""wpf""],
  ""details"": {
    ""version"": 1.0,
    ""author"": null
  }
}";

        var result = StructuredDataParser.Parse(json);

        Assert.True(result.Success);
        Assert.Equal("JSON", result.Format);
        Assert.Single(result.RootNodes);

        var root = result.RootNodes[0];
        Assert.Equal(StructuredNodeType.Object, root.NodeType);
        Assert.Equal("$", root.Path);
        Assert.Equal(5, root.Children.Count);

        // id
        var idNode = root.Children[0];
        Assert.Equal("id", idNode.Name);
        Assert.Equal("101", idNode.Value);
        Assert.Equal(StructuredNodeType.Number, idNode.NodeType);
        Assert.Equal("$.id", idNode.Path);

        // name
        var nameNode = root.Children[1];
        Assert.Equal("name", nameNode.Name);
        Assert.Equal("TextForge", nameNode.Value);
        Assert.Equal(StructuredNodeType.String, nameNode.NodeType);

        // tags array
        var tagsNode = root.Children[3];
        Assert.Equal("tags", tagsNode.Name);
        Assert.Equal(StructuredNodeType.Array, tagsNode.NodeType);
        Assert.Equal(2, tagsNode.Children.Count);
        Assert.Equal("utility", tagsNode.Children[0].Value);
        Assert.Equal("$.tags[0]", tagsNode.Children[0].Path);

        // details object
        var detailsNode = root.Children[4];
        Assert.Equal("details", detailsNode.Name);
        Assert.Equal(StructuredNodeType.Object, detailsNode.NodeType);
        Assert.Equal(2, detailsNode.Children.Count);
        Assert.Equal(StructuredNodeType.Null, detailsNode.Children[1].NodeType);
    }

    [Fact]
    public void Parse_ValidXml_CreatesHierarchicalTreeWithAttributesAndElements()
    {
        string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<catalog store=""Main"">
  <book id=""bk101"">
    <title>XML Guide</title>
    <price>44.95</price>
  </book>
  <book id=""bk102"">
    <title>WPF in Action</title>
    <price>39.99</price>
  </book>
</catalog>";

        var result = StructuredDataParser.Parse(xml);

        Assert.True(result.Success);
        Assert.Equal("XML", result.Format);
        Assert.Single(result.RootNodes);

        var root = result.RootNodes[0];
        Assert.Equal("<catalog>", root.Name);
        Assert.Equal(StructuredNodeType.Element, root.NodeType);
        Assert.Equal("/catalog", root.Path);

        // Attribute @store and books
        Assert.Contains(root.Children, c => c.Name == "@store" && c.Value == "Main" && c.NodeType == StructuredNodeType.Attribute);

        var books = root.Children.Where(c => c.Name == "<book>").ToList();
        Assert.Equal(2, books.Count);

        var firstBook = books[0];
        Assert.Equal("/catalog/book[1]", firstBook.Path);
        Assert.Contains(firstBook.Children, c => c.Name == "@id" && c.Value == "bk101");
        Assert.Contains(firstBook.Children, c => c.Name == "<title>" && c.Value == "XML Guide");
    }

    [Fact]
    public void Parse_ValidYaml_CreatesHierarchicalTree()
    {
        string yaml = @"
server:
  host: localhost
  port: 8080
  secure: true
routes:
  - /api/v1
  - /api/v2
";

        var result = StructuredDataParser.Parse(yaml);

        Assert.True(result.Success);
        Assert.Equal("YAML", result.Format);
        Assert.Single(result.RootNodes);

        var root = result.RootNodes[0];
        Assert.Equal(StructuredNodeType.Object, root.NodeType);

        var serverNode = root.Children.FirstOrDefault(c => c.Name == "server");
        Assert.NotNull(serverNode);
        Assert.Equal(StructuredNodeType.Object, serverNode.NodeType);
        Assert.Contains(serverNode.Children, c => c.Name == "host" && c.Value == "localhost");
        Assert.Contains(serverNode.Children, c => c.Name == "port" && c.Value == "8080");
        Assert.Contains(serverNode.Children, c => c.Name == "secure" && c.Value == "true");

        var routesNode = root.Children.FirstOrDefault(c => c.Name == "routes");
        Assert.NotNull(routesNode);
        Assert.Equal(StructuredNodeType.Array, routesNode.NodeType);
        Assert.Equal(2, routesNode.Children.Count);
        Assert.Equal("/api/v1", routesNode.Children[0].Value);
        Assert.Equal("/api/v2", routesNode.Children[1].Value);
    }

    [Fact]
    public void ApplyFilter_MatchesKeyAndExpandsParents()
    {
        string json = @"
{
  ""user"": {
    ""profile"": {
      ""secretCode"": ""12345"",
      ""nickname"": ""forge_master""
    }
  }
}";

        var result = StructuredDataParser.Parse(json);
        Assert.True(result.Success);
        var root = result.RootNodes[0];

        // Apply filter for 'secretCode'
        root.ApplyFilter("secretCode");

        Assert.True(root.IsVisible);
        Assert.True(root.IsExpanded);

        var userNode = root.Children[0];
        Assert.True(userNode.IsVisible);
        Assert.True(userNode.IsExpanded);

        var profileNode = userNode.Children[0];
        Assert.True(profileNode.IsVisible);
        Assert.True(profileNode.IsExpanded);

        var secretNode = profileNode.Children.First(c => c.Name == "secretCode");
        Assert.True(secretNode.IsVisible);

        var nickNode = profileNode.Children.First(c => c.Name == "nickname");
        Assert.False(nickNode.IsVisible);
    }

    [Fact]
    public void ExpandAll_And_CollapseAll_RecursivelyUpdatesAllNodes()
    {
        string json = "{\"a\":{\"b\":{\"c\":1}}}";
        var result = StructuredDataParser.Parse(json);
        var root = result.RootNodes[0];

        root.CollapseAll();
        Assert.False(root.IsExpanded);
        Assert.False(root.Children[0].IsExpanded);
        Assert.False(root.Children[0].Children[0].IsExpanded);

        root.ExpandAll();
        Assert.True(root.IsExpanded);
        Assert.True(root.Children[0].IsExpanded);
        Assert.True(root.Children[0].Children[0].IsExpanded);
    }
}
