using InvoiceDropper.Core.Extraction;

namespace InvoiceDropper.Core.Tests;

public sealed class MockInvoiceExtractionProviderTests
{
    [Fact]
    public async Task LegacyRegexProviderParsesTheBaselineSample()
    {
        var repoRoot = FindRepoRoot();
        var sampleRoot = Path.Combine(repoRoot, "sample-data", "invoices");
        var samplePath = Path.Combine(sampleRoot, "contoso-office-supplies.txt");
        var provider = new LegacyRegexInvoiceExtractionProvider();

        var result = await provider.ExtractAsync(new InvoiceExtractionRequest(
            samplePath,
            Path.GetFileName(samplePath),
            await File.ReadAllTextAsync(samplePath)));

        Assert.Equal("INV-2026-0619", result.InvoiceNumber);
        Assert.Equal("Legacy regex baseline", result.Provider);
        Assert.Equal("regex", result.Model);
        Assert.Equal(3, result.LineItems.Count);
    }

    [Fact]
    public async Task ProviderReturnsDeterministicFixtureAndClipboardText()
    {
        var repoRoot = FindRepoRoot();
        var sampleRoot = Path.Combine(repoRoot, "sample-data", "invoices");
        var provider = new MockInvoiceExtractionProvider(sampleRoot);

        var result = await provider.ExtractAsync(new InvoiceExtractionRequest(
            Path.Combine(sampleRoot, "contoso-office-supplies.invoice.json"),
            "contoso-office-supplies.invoice.json",
            "fixture"));

        Assert.Equal("INV-2026-0619", result.InvoiceNumber);
        Assert.Equal("Deterministic demo mode", result.Provider);
        Assert.Contains("Invoice INV-2026-0619", result.ToClipboardText());
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