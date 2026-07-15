using System.Net;
using System.Text.Json;
using KonXProWebApp.Functions.Models;
using KonXProWebApp.Functions.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace KonXProWebApp.Functions.Tests.Services;

public class SocrataClientTests
{
    private static SocrataClient CreateClient(HttpMessageHandler handler)
    {
        var http = new HttpClient(handler) { BaseAddress = new Uri("https://data.cityofnewyork.us/resource/") };
        var logger = new Mock<ILogger<SocrataClient>>();
        return new SocrataClient(http, logger.Object);
    }

    [Fact]
    public async Task GetPermitsSince_SinglePage_YieldsAllRecords()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new SocrataPermitRecord { JobNumber = "1", DobRunDate = "2024-01-01T00:00:00.000" },
            new SocrataPermitRecord { JobNumber = "2", DobRunDate = "2024-01-01T00:00:00.000" },
            new SocrataPermitRecord { JobNumber = "3", DobRunDate = "2024-01-01T00:00:00.000" }
        });

        var handler = new StaticJsonHandler(json);
        var client = CreateClient(handler);

        var results = new List<SocrataPermitRecord>();
        await foreach (var r in client.GetPermitsSince(null))
            results.Add(r);

        Assert.Equal(3, results.Count);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetPermitsSince_EmptyPage_YieldsNoRecords()
    {
        var handler = new StaticJsonHandler("[]");
        var client = CreateClient(handler);

        var results = new List<SocrataPermitRecord>();
        await foreach (var r in client.GetPermitsSince(null))
            results.Add(r);

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetPermitsSince_WithSince_BuildsWhereClauseWithDateColumn()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        var since = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);
        await foreach (var _ in client.GetPermitsSince(since)) { }

        Assert.NotNull(handler.LastRequestUri);
        var decoded = Uri.UnescapeDataString(handler.LastRequestUri!);
        Assert.Contains("dobrundate", decoded, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("2024-06-01", decoded);
    }

    [Fact]
    public async Task GetPermitsSince_NoSince_OmitsWhereClause()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        await foreach (var _ in client.GetPermitsSince(null)) { }

        Assert.DoesNotContain("$where", handler.LastRequestUri);
    }

    [Fact]
    public async Task GetPermitsSince_RateLimit_RetriesAndSucceeds()
    {
        var handler = new RetryAfterHandler(HttpStatusCode.TooManyRequests, "[]");
        var client = CreateClient(handler);

        var results = new List<SocrataPermitRecord>();
        await foreach (var r in client.GetPermitsSince(null))
            results.Add(r);

        Assert.Equal(2, handler.CallCount);
    }

    [Fact]
    public async Task GetPermitsSince_ConsistentHttpFailure_ThrowsAfterMaxRetries()
    {
        var handler = new AlwaysFailHandler(HttpStatusCode.InternalServerError);
        var client = CreateClient(handler);

        await Assert.ThrowsAsync<HttpRequestException>(async () =>
        {
            await foreach (var _ in client.GetPermitsSince(null)) { }
        });

        // MaxRetries is 3 — should have attempted exactly 3 times before giving up.
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task GetContractorsSince_IncludesBusinessCategoryFilter()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        await foreach (var _ in client.GetContractorsSince(null)) { }

        var decoded = Uri.UnescapeDataString(handler.LastRequestUri!);
        Assert.Contains("Home Improvement Contractor", decoded);
    }

    [Fact]
    public async Task GetBuilding311ComplaintsSince_IncludesBblAndComplaintTypeFilter()
    {
        var handler = new CapturingHandler("[]");
        var client = CreateClient(handler);

        await foreach (var _ in client.GetBuilding311ComplaintsSince(null)) { }

        var decoded = Uri.UnescapeDataString(handler.LastRequestUri!);
        Assert.Contains("bbl IS NOT NULL", decoded);
    }

    // ── Fake handlers ──

    private class StaticJsonHandler : HttpMessageHandler
    {
        private readonly string _json;
        public int CallCount { get; private set; }

        public StaticJsonHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class CapturingHandler : HttpMessageHandler
    {
        private readonly string _json;
        public string? LastRequestUri { get; private set; }

        public CapturingHandler(string json) => _json = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestUri = request.RequestUri?.ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_json, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class RetryAfterHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _firstStatus;
        private readonly string _secondJson;
        public int CallCount { get; private set; }

        public RetryAfterHandler(HttpStatusCode firstStatus, string secondJson)
        {
            _firstStatus = firstStatus;
            _secondJson = secondJson;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            if (CallCount == 1)
            {
                return Task.FromResult(new HttpResponseMessage(_firstStatus));
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_secondJson, System.Text.Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        }
    }

    private class AlwaysFailHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        public int CallCount { get; private set; }

        public AlwaysFailHandler(HttpStatusCode status) => _status = status;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(new HttpResponseMessage(_status));
        }
    }
}
