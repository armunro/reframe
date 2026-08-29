using System.Net;
using System.Net.Http;
using System.Text;
using Reframe.Core.Actions;
using Reframe.Core.Http;
using Reframe.ViewModels;
using Xunit;

namespace Reframe.Tests;

public class WebRequestAndHttpTests
{
    [Fact]
    public void HttpRequestDefinition_InitialProperties_AreCorrect()
    {
        var req = new HttpRequestDefinition
        {
            Method = "GET",
            Url = "https://api.github.com/users/octocat"
        };

        Assert.Equal("GET", req.Method);
        Assert.Equal("https://api.github.com/users/octocat", req.Url);
        Assert.Empty(req.Headers);
        Assert.Null(req.Body);
    }

    [Fact]
    public void HttpRequestDefinition_PostWithHeadersAndBody_SetsPropertiesCorrectly()
    {
        var req = new HttpRequestDefinition
        {
            Method = "POST",
            Url = "https://api.example.com/items",
            Body = "{\"name\":\"Item A\",\"price\":99.9}"
        };
        req.Headers["Authorization"] = "Bearer token123";
        req.Headers["Content-Type"] = "application/json";

        Assert.Equal("POST", req.Method);
        Assert.Equal("https://api.example.com/items", req.Url);
        Assert.Equal("Bearer token123", req.Headers["Authorization"]);
        Assert.Equal("application/json", req.Headers["Content-Type"]);
        Assert.Equal("{\"name\":\"Item A\",\"price\":99.9}", req.Body);
    }

    [Fact]
    public void HttpRequestDefinition_ToCurlCommand_GeneratesValidCurl()
    {
        var req = new HttpRequestDefinition
        {
            Method = "POST",
            Url = "https://api.example.com/submit",
            Body = "{\"test\":true}"
        };
        req.Headers["Authorization"] = "Bearer token";

        string curl = req.ToCurlCommand();
        Assert.StartsWith("curl", curl);
        Assert.Contains("-X POST", curl);
        Assert.Contains("https://api.example.com/submit", curl);
        Assert.Contains("-H \"Authorization: Bearer token\"", curl);
        Assert.Contains("-d \"{\\\"test\\\":true}\"", curl);
    }

    [Fact]
    public void HttpRequestDefinition_ToCSharpSnippet_GeneratesValidCSharp()
    {
        var req = new HttpRequestDefinition
        {
            Method = "POST",
            Url = "https://api.example.com/submit",
            Body = "{\"test\":true}"
        };
        req.Headers["Authorization"] = "Bearer token";

        string cs = req.ToCSharpSnippet();
        Assert.Contains("using var client = new HttpClient();", cs);
        Assert.Contains("HttpMethod.Post", cs);
        Assert.Contains("https://api.example.com/submit", cs);
        Assert.Contains("request.Headers.TryAddWithoutValidation(\"Authorization\", \"Bearer token\");", cs);
    }

    [Fact]
    public async Task HttpRequestExecutor_WithMockHandler_ExecutesSuccessfully()
    {
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[{\"id\": 1, \"name\": \"Alice\"}]", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var executor = new HttpRequestExecutor(handler);
        var req = new HttpRequestDefinition { Method = "GET", Url = "https://mock.example.com/users" };
        var result = await executor.ExecuteAsync(req);

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.StatusCode);
        Assert.Equal("[{\"id\": 1, \"name\": \"Alice\"}]", result.Content);
        Assert.Contains("200", result.SummaryText);
    }

    [Fact]
    public async Task HttpRequestExecutor_ExecuteUrlAsync_ExecutesGetRequest()
    {
        HttpRequestMessage? intercepted = null;
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            intercepted = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"status\":\"ok\"}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var executor = new HttpRequestExecutor(handler);
        var result = await executor.ExecuteAsync("https://api.example.com/status");

        Assert.True(result.IsSuccess);
        Assert.NotNull(intercepted);
        Assert.Equal(HttpMethod.Get, intercepted.Method);
        Assert.Equal("https://api.example.com/status", intercepted.RequestUri?.ToString());
    }

    [Fact]
    public async Task MainViewModel_ExecuteWebRequestAsync_PopulatesInputTextAndHistory()
    {
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"message\": \"success\", \"code\": 200}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var vm = new MainViewModel();
        vm.RequestExecutor = new HttpRequestExecutor(handler);

        vm.WebRequestUrl = "https://api.example.com/test";
        vm.WebRequestMethod = "GET";
        vm.WebRequestDestination = "Input";

        var result = await vm.ExecuteWebRequestAsync();

        Assert.True(result.IsSuccess);
        Assert.Contains("success", vm.InputText);
        Assert.False(vm.IsWebRequestDialogOpen);
        Assert.Contains("Fetched GET", vm.StatusMessage);
    }

    [Fact]
    public async Task MainViewModel_ExecuteWebRequestAsync_WithConfiguredHeadersAndBody()
    {
        HttpRequestMessage? interceptedRequest = null;
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            interceptedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"received\": true}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var vm = new MainViewModel();
        vm.RequestExecutor = new HttpRequestExecutor(handler);

        vm.WebRequestUrl = "https://api.example.com/echo";
        vm.WebRequestMethod = "POST";
        vm.WebRequestHeaders = "X-Test: MyHeader\nAuthorization: Bearer token123";
        vm.WebRequestBody = "{\"data\":\"foo\"}";

        var result = await vm.ExecuteWebRequestAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(interceptedRequest);
        Assert.Equal(HttpMethod.Post, interceptedRequest.Method);
        Assert.Equal("https://api.example.com/echo", interceptedRequest.RequestUri?.ToString());
        Assert.Equal("MyHeader", interceptedRequest.Headers.GetValues("X-Test").FirstOrDefault());
    }

    [Fact]
    public async Task MainViewModel_ExecuteWebRequestAsync_WithDirectUrl_ExecutesDirectly()
    {
        HttpRequestMessage? interceptedRequest = null;
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            interceptedRequest = request;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"direct\": true}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var vm = new MainViewModel();
        vm.RequestExecutor = new HttpRequestExecutor(handler);

        var result = await vm.ExecuteWebRequestAsync("https://direct.example.com/api");

        Assert.True(result.IsSuccess);
        Assert.NotNull(interceptedRequest);
        Assert.Equal("https://direct.example.com/api", interceptedRequest.RequestUri?.ToString());
    }

    [Fact]
    public void MainViewModel_LoadWebRequestPreset_SetsFieldsCorrectly()
    {
        var vm = new MainViewModel();
        vm.LoadWebRequestPreset("httpbin_post");

        Assert.Equal("POST", vm.WebRequestMethod);
        Assert.Equal("https://httpbin.org/post", vm.WebRequestUrl);
        Assert.Contains("application/json", vm.WebRequestHeaders);
        Assert.Contains("Reframe", vm.WebRequestBody);
    }

    [Fact]
    public void ActionRegistry_ContainsFetchWebRequestAction()
    {
        var action = ActionRegistry.AllActions.FirstOrDefault(a => a.Id == "FetchWebRequest");
        Assert.NotNull(action);
        Assert.Equal("Fetch from Web / HTTP...", action.Title);
        Assert.Equal("Ctrl+U", action.Shortcut);

        var searchMatches = ActionRegistry.Search("http");
        Assert.Contains(searchMatches, a => a.Id == "FetchWebRequest");
    }

    [Fact]
    public void WebRequestHistoryItem_PropertiesAndDisplays_AreCorrect()
    {
        var item = new WebRequestHistoryItem
        {
            Method = "POST",
            Url = "https://api.example.com/v1/users",
            Headers = "Authorization: Bearer token\nContent-Type: application/json",
            Body = "{\"name\":\"test\"}",
            Destination = "Input",
            Timestamp = DateTime.Now,
            IsSuccess = true,
            StatusCode = 201,
            StatusDescription = "Created",
            ResponseSummary = "201 Created (120 B)",
            DurationMs = 85
        };

        Assert.Equal("POST", item.Method);
        Assert.Equal("https://api.example.com/v1/users", item.Url);
        Assert.Equal("api.example.com", item.HostDisplay);
        Assert.Equal("[POST] https://api.example.com/v1/users", item.DisplayTitle);
        Assert.True(item.HasHeaders);
        Assert.True(item.HasBody);
        Assert.Equal("2 headers", item.HeadersCountDisplay);
        Assert.Equal("AccentWarningBrush", item.MethodBadgeBrushName);
        Assert.Equal(85, item.DurationMs);
    }

    [Fact]
    public async Task MainViewModel_ExecuteWebRequestAsync_RecordsInHistoryAndAllowsReuse()
    {
        var handler = new MockHttpMessageHandler((request, cancellationToken) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"item\": \"ok\"}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var vm = new MainViewModel();
        vm.RequestExecutor = new HttpRequestExecutor(handler);

        vm.WebRequestUrl = "https://api.example.com/custom-endpoint";
        vm.WebRequestMethod = "GET";
        vm.WebRequestHeaders = "Accept: application/json";
        vm.WebRequestBody = "";
        vm.WebRequestDestination = "Input";

        int countBefore = vm.WebRequestHistory.Count;
        var result = await vm.ExecuteWebRequestAsync();

        Assert.True(result.IsSuccess);
        Assert.True(vm.HasWebRequestHistory);
        Assert.Equal(countBefore + 1, vm.WebRequestHistory.Count);

        var top = vm.WebRequestHistory[0];
        Assert.Equal("GET", top.Method);
        Assert.Equal("https://api.example.com/custom-endpoint", top.Url);
        Assert.True(top.IsSuccess);

        // Modify VM state to something else
        vm.WebRequestUrl = "https://other.com";
        vm.WebRequestMethod = "POST";
        vm.WebRequestHeaders = "Header: 123";

        // Load the history item back
        vm.LoadWebRequestHistoryItemCommand.Execute(top);

        Assert.Equal("GET", vm.WebRequestMethod);
        Assert.Equal("https://api.example.com/custom-endpoint", vm.WebRequestUrl);
        Assert.Equal("Accept: application/json", vm.WebRequestHeaders);
        Assert.Equal(0, vm.SelectedWebRequestTabIndex);
    }

    [Fact]
    public void MainViewModel_DeleteAndClearWebRequestHistory_WorksCorrectly()
    {
        var vm = new MainViewModel();
        Assert.True(vm.WebRequestHistory.Count > 0);

        var firstItem = vm.WebRequestHistory[0];
        int initialCount = vm.WebRequestHistory.Count;

        vm.DeleteWebRequestHistoryItemCommand.Execute(firstItem);
        Assert.Equal(initialCount - 1, vm.WebRequestHistory.Count);

        vm.ClearWebRequestHistoryCommand.Execute(null);
        Assert.Empty(vm.WebRequestHistory);
        Assert.False(vm.HasWebRequestHistory);
        Assert.Null(vm.SelectedWebRequestHistoryItem);
    }

    [Fact]
    public void MainViewModel_WebRequestHistoryView_FiltersByUrlOrMethod()
    {
        var vm = new MainViewModel();
        vm.WebRequestHistory.Clear();

        vm.WebRequestHistory.Add(new WebRequestHistoryItem
        {
            Method = "GET",
            Url = "https://api.github.com/users",
            ResponseSummary = "200 OK"
        });
        vm.WebRequestHistory.Add(new WebRequestHistoryItem
        {
            Method = "POST",
            Url = "https://httpbin.org/post",
            ResponseSummary = "200 OK"
        });

        vm.WebRequestHistoryFilter = "github";
        var filteredList = vm.WebRequestHistoryView.Cast<WebRequestHistoryItem>().ToList();
        Assert.Single(filteredList);
        Assert.Equal("https://api.github.com/users", filteredList[0].Url);

        vm.WebRequestHistoryFilter = "POST";
        filteredList = vm.WebRequestHistoryView.Cast<WebRequestHistoryItem>().ToList();
        Assert.Single(filteredList);
        Assert.Equal("https://httpbin.org/post", filteredList[0].Url);

        vm.WebRequestHistoryFilter = "";
        filteredList = vm.WebRequestHistoryView.Cast<WebRequestHistoryItem>().ToList();
        Assert.Equal(2, filteredList.Count);
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public MockHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
