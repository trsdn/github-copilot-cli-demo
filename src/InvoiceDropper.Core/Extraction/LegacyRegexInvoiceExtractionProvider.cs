using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace InvoiceDropper.Core.Extraction;

public sealed class LegacyRegexInvoiceExtractionProvider : IInvoiceExtractionProvider
{
    public string Name => "Legacy regex baseline";

    public Task<InvoiceExtractionResult> ExtractAsync(InvoiceExtractionRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var text = request.ContentText;
        var invoiceNumber = MatchValue(text, @"^Invoice:\s*(?<value>\S+)");
        var invoiceDate = MatchDate(text, @"^Invoice Date:\s*(?<value>\d{4}-\d{2}-\d{2})");
        var dueDate = MatchDate(text, @"^Due Date:\s*(?<value>\d{4}-\d{2}-\d{2})");
        var purchaseOrder = MatchValue(text, @"^PO:\s*(?<value>\S+)");
        var subtotal = MatchDecimal(text, @"^Subtotal:\s*(?<value>\d+(?:\.\d{2})?)");
        var tax = MatchDecimal(text, @"^Tax:\s*(?<value>\d+(?:\.\d{2})?)");
        var shipping = MatchDecimal(text, @"^Shipping:\s*(?<value>\d+(?:\.\d{2})?)");
        var total = MatchDecimal(text, @"^Total:\s*(?<value>\d+(?:\.\d{2})?)");
        var currency = MatchValue(text, @"^Total:\s*\d+(?:\.\d{2})?\s*(?<value>[A-Z]{3})");
        var paymentTerms = MatchValue(text, @"^Payment Terms:\s*(?<value>.+)");
        var paymentReference = MatchValue(text, @"^Payment Reference:\s*(?<value>\S+)");
        var vendorName = FirstNonEmptyLine(text);
        var customerName = MatchValue(text, @"Bill To:\s*(?<value>[^,\r\n]+)");
        var lineItems = ParseLineItems(text).ToArray();

        stopwatch.Stop();
        var result = new InvoiceExtractionResult(
            invoiceNumber,
            new Address(TitleCase(vendorName), string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            new Address(customerName, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
            invoiceDate,
            dueDate,
            purchaseOrder,
            lineItems,
            subtotal,
            tax,
            shipping,
            total,
            currency,
            paymentTerms,
            paymentReference,
            0.72m,
            Name,
            "regex",
            request.SourceFileName,
            stopwatch.Elapsed);

        // Legacy baseline is best-effort: return whatever the regex matched
        // (often nothing for PDFs or oddly shaped text) instead of throwing.
        return Task.FromResult(result);
    }

    private static IEnumerable<InvoiceLineItem> ParseLineItems(string text)
    {
        var matches = Regex.Matches(text, @"-\s*(?<description>.+?),\s*qty\s*(?<quantity>\d+(?:\.\d+)?),\s*unit\s*(?<unit>\d+(?:\.\d{2})?),\s*amount\s*(?<amount>\d+(?:\.\d{2})?)", RegexOptions.IgnoreCase);
        foreach (Match match in matches)
        {
            yield return new InvoiceLineItem(
                match.Groups["description"].Value.Trim(),
                decimal.Parse(match.Groups["quantity"].Value, CultureInfo.InvariantCulture),
                decimal.Parse(match.Groups["unit"].Value, CultureInfo.InvariantCulture),
                decimal.Parse(match.Groups["amount"].Value, CultureInfo.InvariantCulture));
        }
    }

    private static string MatchValue(string text, string pattern)
    {
        var match = Regex.Match(text, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);
        return match.Success ? match.Groups["value"].Value.Trim() : string.Empty;
    }

    private static decimal MatchDecimal(string text, string pattern)
    {
        var value = MatchValue(text, pattern);
        return decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;
    }

    private static DateOnly MatchDate(string text, string pattern)
    {
        var value = MatchValue(text, pattern);
        return DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed) ? parsed : default;
    }

    private static string FirstNonEmptyLine(string text)
    {
        return text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? string.Empty;
    }

    private static string TitleCase(string value)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());
    }
}