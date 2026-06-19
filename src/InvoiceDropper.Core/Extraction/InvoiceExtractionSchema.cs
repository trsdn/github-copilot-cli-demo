namespace InvoiceDropper.Core.Extraction;

public static class InvoiceExtractionSchema
{
    public static IReadOnlyList<string> Validate(InvoiceExtractionResult result)
    {
        var errors = new List<string>();

        Required(result.InvoiceNumber, "invoiceNumber", errors);
        Required(result.Vendor?.Name, "vendor.name", errors);
        Required(result.Customer?.Name, "customer.name", errors);
        Required(result.PurchaseOrderNumber, "purchaseOrderNumber", errors);
        Required(result.Currency, "currency", errors);
        Required(result.PaymentTerms, "paymentTerms", errors);
        Required(result.PaymentReference, "paymentReference", errors);

        if (result.LineItems is null || result.LineItems.Count == 0)
        {
            errors.Add("lineItems must contain at least one item.");
            return errors;
        }

        var lineTotal = result.LineItems.Sum(item => item.Amount);
        if (Math.Abs(lineTotal - result.Subtotal) > 0.01m)
        {
            errors.Add($"subtotal {result.Subtotal} does not match line item total {lineTotal}.");
        }

        var computedTotal = result.Subtotal + result.Tax + result.Shipping;
        if (Math.Abs(computedTotal - result.Total) > 0.01m)
        {
            errors.Add($"total {result.Total} does not match subtotal + tax + shipping {computedTotal}.");
        }

        if (result.Confidence is < 0 or > 1)
        {
            errors.Add("confidence must be between 0 and 1.");
        }

        if (result.DueDate < result.InvoiceDate)
        {
            errors.Add("dueDate must be on or after invoiceDate.");
        }

        return errors;
    }

    public static void ThrowIfInvalid(InvoiceExtractionResult result)
    {
        var errors = Validate(result);
        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors));
        }
    }

    private static void Required(string? value, string field, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{field} is required.");
        }
    }
}