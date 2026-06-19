using System.Diagnostics;
using System.Text.Json;

namespace InvoiceDropper.Core.Extraction;

public sealed class MockInvoiceExtractionProvider(string sampleDataRoot) : IInvoiceExtractionProvider
{
    public string Name => "Deterministic demo mode";

    public async Task<InvoiceExtractionResult> ExtractAsync(InvoiceExtractionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var fixturePath = ResolveFixturePath(request.SourceFilePath, request.SourceFileName);
        await using var stream = File.OpenRead(fixturePath);
        var result = await JsonSerializer.DeserializeAsync<InvoiceExtractionResult>(stream, InvoiceExtractionResult.JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException($"Could not read invoice fixture '{fixturePath}'.");

        stopwatch.Stop();
        result = result with
        {
            Provider = Name,
            Model = "fixture",
            SourceFileName = request.SourceFileName,
            Elapsed = stopwatch.Elapsed
        };

        InvoiceExtractionSchema.ThrowIfInvalid(result);
        return result;
    }

    private string ResolveFixturePath(string sourceFilePath, string sourceFileName)
    {
        if (Path.GetExtension(sourceFilePath).Equals(".json", StringComparison.OrdinalIgnoreCase) && File.Exists(sourceFilePath))
        {
            return sourceFilePath;
        }

        var baseName = Path.GetFileNameWithoutExtension(sourceFileName);
        var direct = Path.Combine(sampleDataRoot, $"{baseName}.invoice.json");
        if (File.Exists(direct))
        {
            return direct;
        }

        var first = Directory.EnumerateFiles(sampleDataRoot, "*.invoice.json").OrderBy(path => path).FirstOrDefault();
        return first ?? throw new FileNotFoundException("No sample invoice fixtures were found.", sampleDataRoot);
    }
}