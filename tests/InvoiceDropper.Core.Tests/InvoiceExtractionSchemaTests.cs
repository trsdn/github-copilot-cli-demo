using System.Text.Json;
using InvoiceDropper.Core.Extraction;

namespace InvoiceDropper.Core.Tests;

public sealed class InvoiceExtractionSchemaTests
{
    [Fact]
    public async Task SampleInvoicesValidate()
    {
        var repoRoot = FindRepoRoot();
        var sampleRoot = Path.Combine(repoRoot, "sample-data", "invoices");
        var samples = Directory.EnumerateFiles(sampleRoot, "*.invoice.json").ToArray();

        Assert.NotEmpty(samples);
        foreach (var sample in samples)
        {
            await using var stream = File.OpenRead(sample);
            var result = await JsonSerializer.DeserializeAsync<InvoiceExtractionResult>(stream, InvoiceExtractionResult.JsonOptions);

            Assert.NotNull(result);
            Assert.Empty(InvoiceExtractionSchema.Validate(result!));
        }
    }

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "InvoiceDropperDemo.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root was not found.");
    }
}