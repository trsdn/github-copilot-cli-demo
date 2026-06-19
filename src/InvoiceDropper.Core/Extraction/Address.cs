namespace InvoiceDropper.Core.Extraction;

public sealed record Address(
    string Name,
    string Line1,
    string City,
    string Region,
    string PostalCode,
    string Country);