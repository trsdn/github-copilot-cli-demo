using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

namespace InvoiceDropper.Core.Extraction;

public sealed class CopilotSdkInvoiceExtractionProvider(HttpClient httpClient, IInvoiceExtractionProvider fallbackProvider) : IInvoiceExtractionProvider
{
    public string Name => "Copilot SDK live";

    public async Task<InvoiceExtractionResult> ExtractAsync(InvoiceExtractionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            var response = await httpClient.PostAsJsonAsync("/extract", new
            {
                sourceFileName = request.SourceFileName,
                contentText = request.ContentText,
                preferredModel = request.PreferredModel
            }, InvoiceExtractionResult.JsonOptions, timeout.Token);

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<InvoiceExtractionResult>(InvoiceExtractionResult.JsonOptions, timeout.Token)
                ?? throw new InvalidOperationException("Copilot SDK bridge returned an empty response.");

            stopwatch.Stop();
            result = result with { Provider = Name, SourceFileName = request.SourceFileName, Elapsed = stopwatch.Elapsed };
            InvoiceExtractionSchema.ThrowIfInvalid(result);
            return result;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return await fallbackProvider.ExtractAsync(request, cancellationToken);
        }
    }
}