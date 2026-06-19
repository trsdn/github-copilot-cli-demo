namespace InvoiceDropper.Core.Extraction;

public sealed record InvoiceExtractionRequest(
    string SourceFilePath,
    string SourceFileName,
    string ContentText,
    string PreferredModel = "default");