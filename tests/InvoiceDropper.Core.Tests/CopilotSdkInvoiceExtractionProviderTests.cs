using System.Net;
using System.Net.Http.Json;
using InvoiceDropper.Core.Extraction;

namespace InvoiceDropper.Core.Tests;

public sealed class CopilotSdkInvoiceExtractionProviderTests
{
    [Fact]
    public async Task FallsBackToFixtureWhenBridgeIsUnreachable()
    {
        var fallback = new MockInvoiceExtractionProvider(SampleRoot());
        var httpClient = new HttpClient(new ThrowingHandler())
        {
            BaseAddress = new Uri("http://127.0.0.1:48731")
        };
        var provider = new CopilotSdkInvoiceExtractionProvider(httpClient, fallback);

        var result = await provider.ExtractAsync(new InvoiceExtractionRequest(
            Path.Combine(SampleRoot(), "contoso-office-supplies.invoice.json"),
            "contoso-office-supplies.invoice.json",
            "fixture"));

        Assert.Equal("Deterministic demo mode", result.Provider);
        Assert.Equal("INV-2026-0619", result.InvoiceNumber);
    }

    [Fact]
    public async Task ReturnsBridgeResultWhenExtractionSucceeds()
    {
        var fallback = new MockInvoiceExtractionProvider(SampleRoot());
        var bridgeResult = BuildValidResult();
        var httpClient = new HttpClient(new StubJsonHandler(HttpStatusCode.OK, bridgeResult))
        {
            BaseAddress = new Uri("http://127.0.0.1:48731")
        };
        var provider = new CopilotSdkInvoiceExtractionProvider(httpClient, fallback);

        var result = await provider.ExtractAsync(new InvoiceExtractionRequest(
            "memory://acme.txt",
            "acme.txt",
            "invoice text"));

        Assert.Equal("Copilot SDK live", result.Provider);
        Assert.Equal("INV-ACME-1", result.InvoiceNumber);
        Assert.Equal("acme.txt", result.SourceFileName);
    }

    private static InvoiceExtractionResult BuildValidResult()
    {
        var address = new Address("Acme Co", "1 Way", "Town", "WA", "98000", "USA");
        var lineItems = new List<InvoiceLineItem>
        {
            new("Widget", 2m, 50m, 100m)
        };
        return new InvoiceExtractionResult(
            "INV-ACME-1",
            address,
            address with { Name = "Buyer Inc" },
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 31),
            "PO-1",
            lineItems,
            Subtotal: 100m,
            Tax: 10m,
            Shipping: 5m,
            Total: 115m,
            Currency: "USD",
            PaymentTerms: "Net 30",
            PaymentReference: "REF-1",
            Confidence: 0.9m,
            Provider: "bridge",
            Model: "mock",
            SourceFileName: "acme.txt",
            Elapsed: TimeSpan.Zero);
    }

    private static string SampleRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "InvoiceDropperDemo.sln")))
        {
            directory = directory.Parent;
        }

        var repoRoot = directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
        return Path.Combine(repoRoot, "sample-data", "invoices");
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("bridge unreachable");
    }

    private sealed class StubJsonHandler(HttpStatusCode statusCode, InvoiceExtractionResult body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = JsonContent.Create(body, options: InvoiceExtractionResult.JsonOptions)
            };
            return Task.FromResult(response);
        }
    }
}
