import { performance } from "node:perf_hooks";
import { extractJsonObject, validateInvoice, type ExtractRequest, type InvoiceExtractionResult } from "./schema.js";
import { readFixture } from "./fixtures.js";

export async function extractInvoiceWithSdk(
  session: any | undefined,
  request: ExtractRequest,
  model: string,
): Promise<InvoiceExtractionResult> {
  const start = performance.now();
  if (!session || process.env.COPILOT_BRIDGE_MOCK === "1") {
    const fixture = await readFixture(request.sourceFileName);
    return {
      ...fixture,
      provider: "Copilot SDK bridge mock",
      model: process.env.COPILOT_BRIDGE_MOCK === "1" ? "mock" : "sdk-unavailable",
      sourceFileName: request.sourceFileName ?? fixture.sourceFileName,
      elapsed: performance.now() - start,
    };
  }

  const response = await session.sendAndWait({ prompt: buildPrompt(request) }, 10000);
  const content = response?.data.content;
  if (!content) {
    throw new Error("Copilot SDK did not return an assistant message.");
  }

  const parsed = extractJsonObject(content) as InvoiceExtractionResult;
  const result: InvoiceExtractionResult = {
    ...parsed,
    provider: "GitHub Copilot SDK",
    model,
    sourceFileName: request.sourceFileName ?? parsed.sourceFileName ?? "invoice.txt",
    elapsed: performance.now() - start,
  };

  const errors = validateInvoice(result);
  if (errors.length > 0) {
    throw new Error(`Invoice JSON failed validation: ${errors.join("; ")}`);
  }

  return result;
}

function buildPrompt(request: ExtractRequest): string {
  return `Extract invoice fields from the following synthetic invoice text. Return only one JSON object with camelCase fields: invoiceNumber, vendor, customer, invoiceDate, dueDate, purchaseOrderNumber, lineItems, subtotal, tax, shipping, total, currency, paymentTerms, paymentReference, confidence, provider, model, sourceFileName, elapsed. Use ISO dates and numeric amounts.\n\nSource file: ${request.sourceFileName ?? "invoice.txt"}\n\nInvoice text:\n${request.contentText ?? ""}`;
}