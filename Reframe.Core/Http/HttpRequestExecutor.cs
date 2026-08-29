using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

namespace Reframe.Core.Http;

/// <summary>
/// Executes HTTP requests defined by <see cref="HttpRequestDefinition"/> or direct URLs.
/// </summary>
public class HttpRequestExecutor
{
    private readonly HttpMessageHandler? _customHandler;

    public HttpRequestExecutor(HttpMessageHandler? customHandler = null)
    {
        _customHandler = customHandler;
    }

    /// <summary>
    /// Executes a request for the specified URL with a default GET request.
    /// </summary>
    public Task<HttpResponseResult> ExecuteAsync(string url, CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestDefinition { Url = url };
        return ExecuteAsync(request, cancellationToken);
    }

    /// <summary>
    /// Executes the specified <see cref="HttpRequestDefinition"/>.
    /// </summary>
    public async Task<HttpResponseResult> ExecuteAsync(HttpRequestDefinition request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Url))
        {
            return new HttpResponseResult
            {
                IsSuccess = false,
                StatusCode = 0,
                ErrorMessage = "URL cannot be empty."
            };
        }

        var sw = Stopwatch.StartNew();

        try
        {
            using var handler = CreateHandler(request);
            using var client = new HttpClient(handler, disposeHandler: _customHandler == null)
            {
                Timeout = TimeSpan.FromSeconds(Math.Max(1, request.TimeoutSeconds))
            };

            var httpMethod = new HttpMethod(string.IsNullOrWhiteSpace(request.Method) ? "GET" : request.Method.ToUpperInvariant());
            using var requestMsg = new HttpRequestMessage(httpMethod, request.Url);

            // Populate headers and body
            string? contentType = null;
            foreach (var kvp in request.Headers)
            {
                if (kvp.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    contentType = kvp.Value;
                }
                else if (kvp.Key.Equals("Host", StringComparison.OrdinalIgnoreCase))
                {
                    // Host header is restricted in some .NET versions, TryAddWithoutValidation handles it
                    requestMsg.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
                else
                {
                    requestMsg.Headers.TryAddWithoutValidation(kvp.Key, kvp.Value);
                }
            }

            // Set user agent default if not provided
            if (!request.Headers.ContainsKey("User-Agent"))
            {
                requestMsg.Headers.TryAddWithoutValidation("User-Agent", "Reframe/1.0 (Windows; Data Transformer)");
            }

            // Add Body for methods that support body (or if body is supplied)
            if (!string.IsNullOrEmpty(request.Body))
            {
                var content = new StringContent(request.Body, Encoding.UTF8);
                if (!string.IsNullOrWhiteSpace(contentType))
                {
                    try
                    {
                        content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
                    }
                    catch
                    {
                        content.Headers.TryAddWithoutValidation("Content-Type", contentType);
                    }
                }
                else
                {
                    // Infer JSON if starts with { or [
                    var trimmedBody = request.Body.Trim();
                    if ((trimmedBody.StartsWith('{') && trimmedBody.EndsWith('}')) ||
                        (trimmedBody.StartsWith('[') && trimmedBody.EndsWith(']')))
                    {
                        content.Headers.ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                    }
                }
                requestMsg.Content = content;
            }

            using var response = await client.SendAsync(requestMsg, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var result = new HttpResponseResult
            {
                IsSuccess = response.IsSuccessStatusCode,
                StatusCode = (int)response.StatusCode,
                StatusDescription = response.ReasonPhrase ?? response.StatusCode.ToString(),
                Content = responseBody,
                ContentType = response.Content.Headers.ContentType?.ToString(),
                ContentLength = response.Content.Headers.ContentLength ?? Encoding.UTF8.GetByteCount(responseBody),
                Elapsed = sw.Elapsed
            };

            foreach (var h in response.Headers)
            {
                result.Headers[h.Key] = string.Join(", ", h.Value);
            }
            foreach (var h in response.Content.Headers)
            {
                result.Headers[h.Key] = string.Join(", ", h.Value);
            }

            if (!response.IsSuccessStatusCode)
            {
                result.ErrorMessage = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}";
            }

            return result;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            sw.Stop();
            return new HttpResponseResult
            {
                IsSuccess = false,
                StatusCode = 408,
                StatusDescription = "Request Timeout",
                ErrorMessage = $"Request timed out after {request.TimeoutSeconds}s: {ex.Message}",
                Elapsed = sw.Elapsed
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            return new HttpResponseResult
            {
                IsSuccess = false,
                StatusCode = 0,
                StatusDescription = "Error",
                ErrorMessage = ex.Message,
                Elapsed = sw.Elapsed
            };
        }
    }

    private HttpMessageHandler CreateHandler(HttpRequestDefinition request)
    {
        if (_customHandler != null)
        {
            return _customHandler;
        }

        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = request.FollowRedirects,
            AutomaticDecompression = System.Net.DecompressionMethods.All
        };

        if (request.IgnoreSslErrors)
        {
            handler.ServerCertificateCustomValidationCallback = (_, _, _, _) => true;
        }

        return handler;
    }
}
