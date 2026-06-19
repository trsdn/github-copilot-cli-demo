namespace InvoiceDropper.Core.Extraction;

public interface IInvoiceExtractionProvider
{
    string Name { get; }

    Task<InvoiceExtractionResult> ExtractAsync(InvoiceExtractionRequest request, CancellationToken cancellationToken = default);
}