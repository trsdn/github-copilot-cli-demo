using System.Text.Json;
using System.Text.Json.Serialization;

namespace InvoiceDropper.Core.Extraction;

public sealed record InvoiceExtractionResult(
    string InvoiceNumber,
    Address Vendor,
    Address Customer,
    DateOnly InvoiceDate,
    DateOnly DueDate,
    string PurchaseOrderNumber,
    IReadOnlyList<InvoiceLineItem> LineItems,
    decimal Subtotal,
    decimal Tax,
    decimal Shipping,
    decimal Total,
    string Currency,
    string PaymentTerms,
    string PaymentReference,
    decimal Confidence,
    string Provider,
    string Model,
    string SourceFileName,
    TimeSpan Elapsed)
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new DateOnlyJsonConverter(), new TimeSpanMillisecondsJsonConverter() }
    };

    public string ToClipboardText()
    {
        var summary = $"Invoice {InvoiceNumber} from {Vendor.Name} for {Total:C} {Currency}\n" +
            $"Due {DueDate:yyyy-MM-dd} | PO {PurchaseOrderNumber} | Confidence {Confidence:P0}\n";

        return summary + "\n" + JsonSerializer.Serialize(this, JsonOptions);
    }
}