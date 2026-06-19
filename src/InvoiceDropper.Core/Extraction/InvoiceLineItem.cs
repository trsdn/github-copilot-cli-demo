namespace InvoiceDropper.Core.Extraction;

public sealed record InvoiceLineItem(
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal Amount,
    string? Sku = null);