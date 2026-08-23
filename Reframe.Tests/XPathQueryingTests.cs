using Reframe.Core.Structured;
using Reframe.Core.Transformers;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class XPathQueryingTests
{
    private const string SampleCatalogXml = """
    <catalog store="TechBooks">
      <book id="bk101" category="programming">
        <title>C# in Depth</title>
        <author>Jon Skeet</author>
        <price>44.99</price>
        <inStock>true</inStock>
      </book>
      <book id="bk102" category="architecture">
        <title>Clean Architecture</title>
        <author>Robert C. Martin</author>
        <price>32.50</price>
        <inStock>false</inStock>
      </book>
    </catalog>
    """;

    private const string SampleJsonUsers = """
    {
      "users": [
        { "id": "u1", "name": "Alice", "role": "Admin", "score": 95 },
        { "id": "u2", "name": "Bob", "role": "Dev", "score": 88 }
      ]
    }
    """;

    private const string SampleYamlServers = """
    servers:
      - name: web-01
        role: frontend
        port: 80
      - name: db-01
        role: database
        port: 5432
    """;

    [Fact]
    public void QueryXPath_ElementWildcard_ReturnsAllChildElements()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//book/*");

        Assert.Contains("<title>C# in Depth</title>", result);
        Assert.Contains("<author>Jon Skeet</author>", result);
        Assert.Contains("<price>44.99</price>", result);
        Assert.Contains("<title>Clean Architecture</title>", result);
    }

    [Fact]
    public void QueryXPath_AttributeWildcard_ReturnsAllAttributes()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//@*");

        Assert.Contains("@store=\"TechBooks\"", result);
        Assert.Contains("@id=\"bk101\"", result);
        Assert.Contains("@category=\"programming\"", result);
        Assert.Contains("@id=\"bk102\"", result);
        Assert.Contains("@category=\"architecture\"", result);
    }

    [Fact]
    public void QueryXPath_ElementAttributeWildcard_ReturnsBookAttributes()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//book/@*");

        Assert.Contains("@id=\"bk101\"", result);
        Assert.Contains("@category=\"programming\"", result);
        Assert.Contains("@id=\"bk102\"", result);
        Assert.DoesNotContain("@store", result);
    }

    [Fact]
    public void QueryXPath_PredicateWithPrice_ReturnsMatchingBook()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//book[price > 40]");

        Assert.Contains("bk101", result);
        Assert.Contains("C# in Depth", result);
        Assert.DoesNotContain("Clean Architecture", result);
    }

    [Fact]
    public void QueryXPath_PositionalIndex_ReturnsFirstBook()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//book[1]/title");

        Assert.Equal("<title>C# in Depth</title>", result.Trim());
    }

    [Fact]
    public void QueryXPath_CountFunction_ReturnsNumericCount()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "count(//book)");

        Assert.Equal("2", result.Trim());
    }

    [Fact]
    public void QueryXPath_SumFunction_CalculatesSum()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "sum(//book/price)");

        Assert.Equal("77.49", result.Trim());
    }

    [Fact]
    public void QueryXPath_TextNodeSelector_ReturnsTextValues()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//title/text()");

        Assert.Contains("C# in Depth", result);
        Assert.Contains("Clean Architecture", result);
        Assert.DoesNotContain("<title>", result);
    }

    [Fact]
    public void QueryXPath_XmlWithNamespace_EvaluatesWithoutError()
    {
        string nsXml = """
        <catalog xmlns="http://example.com/books" store="Main">
          <book id="b1">
            <title>Design Patterns</title>
          </book>
        </catalog>
        """;

        string result = StructuredTransformers.QueryXPath(nsXml, "//book/*");

        Assert.Contains("<title>Design Patterns</title>", result);
    }

    [Fact]
    public void QueryXPath_OnJsonData_ExecutesXPathSuccessfully()
    {
        string result = StructuredTransformers.QueryXPath(SampleJsonUsers, "//users/*");

        Assert.Contains("<name>Alice</name>", result);
        Assert.Contains("<name>Bob</name>", result);
    }

    [Fact]
    public void QueryXPath_OnYamlData_ExecutesXPathSuccessfully()
    {
        string result = StructuredTransformers.QueryXPath(SampleYamlServers, "//servers/name");

        Assert.Contains("<name>web-01</name>", result);
        Assert.Contains("<name>db-01</name>", result);
    }

    [Fact]
    public void ExtractXPathValues_ExtractsInnerValues()
    {
        string result = StructuredTransformers.ExtractXPathValues(SampleCatalogXml, "//author");

        Assert.Contains("Jon Skeet", result);
        Assert.Contains("Robert C. Martin", result);
        Assert.DoesNotContain("<author>", result);
    }

    [Fact]
    public void ExtractXPathAttributes_ExtractsAllAttributes()
    {
        string result = StructuredTransformers.ExtractXPathAttributes(SampleCatalogXml);

        Assert.Contains("@store=\"TechBooks\"", result);
        Assert.Contains("@id=\"bk101\"", result);
        Assert.Contains("@category=\"programming\"", result);
    }

    [Fact]
    public void QueryPath_WithXPathWildcard_DelegatesToXPathEngine()
    {
        string result = StructuredTransformers.QueryPath(SampleCatalogXml, "//book/*");

        Assert.Contains("<title>C# in Depth</title>", result);
    }

    [Fact]
    public void QueryXPath_InvalidXPath_ReturnsDescriptiveError()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "///[[invalid");

        Assert.Contains("XPath error", result);
    }

    [Fact]
    public void QueryXPath_NestedWildcardPath_ReturnsGrandchildElements()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "/*/*/*");

        Assert.Contains("<title>C# in Depth</title>", result);
        Assert.Contains("<author>Jon Skeet</author>", result);
        Assert.Contains("<price>44.99</price>", result);
        Assert.Contains("<inStock>true</inStock>", result);
    }

    [Fact]
    public void QueryXPath_WildcardElementsWithAttributes_ReturnsOnlyElementsHavingAttributes()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//*[@category]");

        Assert.Contains("bk101", result);
        Assert.Contains("bk102", result);
        Assert.DoesNotContain("<catalog", result);
    }

    [Fact]
    public void QueryXPath_NonMatchingXPath_ReturnsHelpfulMessage()
    {
        string result = StructuredTransformers.QueryXPath(SampleCatalogXml, "//nonexistent");

        Assert.Equal("No results matching XPath '//nonexistent'", result);
    }

    [Fact]
    public void MainViewModel_XPathActions_ExecuteProperly()
    {
        var vm = new MainViewModel();
        vm.InputText = SampleCatalogXml;

        // 1. QueryXPath
        vm.StructuredQueryPath = "//book/*";
        vm.ActionCommand.Execute("QueryXPath");
        Assert.Contains("<title>C# in Depth</title>", vm.OutputText);

        // 2. ExtractXPathValues
        vm.StructuredQueryPath = "//title";
        vm.ActionCommand.Execute("ExtractXPathValues");
        Assert.Contains("C# in Depth", vm.OutputText);
        Assert.Contains("Clean Architecture", vm.OutputText);

        // 3. ExtractXPathAttributes
        vm.StructuredQueryPath = "//@id";
        vm.ActionCommand.Execute("ExtractXPathAttributes");
        Assert.Contains("@id=\"bk101\"", vm.OutputText);
        Assert.Contains("@id=\"bk102\"", vm.OutputText);
    }
}
