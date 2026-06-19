export type Address = {
  name: string;
  line1: string;
  city: string;
  region: string;
  postalCode: string;
  country: string;
};

export type InvoiceLineItem = {
  description: string;
  quantity: number;
  unitPrice: number;
  amount: number;
  sku?: string | null;
};

export type InvoiceExtractionResult = {
  invoiceNumber: string;
  vendor: Address;
  customer: Address;
  invoiceDate: string;
  dueDate: string;
  purchaseOrderNumber: string;
  lineItems: InvoiceLineItem[];
  subtotal: number;
  tax: number;
  shipping: number;
  total: number;
  currency: string;
  paymentTerms: string;
  paymentReference: string;
  confidence: number;
  provider: string;
  model: string;
  sourceFileName: string;
  elapsed: number;
};

export type ExtractRequest = {
  sourceFileName?: string;
  contentText?: string;
  preferredModel?: string;
};

export function validateInvoice(result: InvoiceExtractionResult): string[] {
  const errors: string[] = [];
  required(result.invoiceNumber, "invoiceNumber", errors);
  required(result.vendor?.name, "vendor.name", errors);
  required(result.customer?.name, "customer.name", errors);
  required(result.purchaseOrderNumber, "purchaseOrderNumber", errors);
  required(result.currency, "currency", errors);
  required(result.paymentTerms, "paymentTerms", errors);
  required(result.paymentReference, "paymentReference", errors);

  if (!Array.isArray(result.lineItems) || result.lineItems.length === 0) {
    errors.push("lineItems must contain at least one item.");
  }

  const subtotal = (result.lineItems ?? []).reduce((sum, item) => sum + Number(item.amount ?? 0), 0);
  if (Number.isNaN(Number(result.subtotal)) || Math.abs(subtotal - Number(result.subtotal)) > 0.01) {
    errors.push(`subtotal ${result.subtotal} does not match line item total ${subtotal}.`);
  }

  const total = Number(result.subtotal) + Number(result.tax) + Number(result.shipping);
  if (Number.isNaN(total) || Math.abs(total - Number(result.total)) > 0.01) {
    errors.push(`total ${result.total} does not match subtotal + tax + shipping ${total}.`);
  }

  if (Number.isNaN(Number(result.confidence)) || Number(result.confidence) < 0 || Number(result.confidence) > 1) {
    errors.push("confidence must be between 0 and 1.");
  }

  if (result.dueDate && result.invoiceDate && new Date(result.dueDate) < new Date(result.invoiceDate)) {
    errors.push("dueDate must be on or after invoiceDate.");
  }

  return errors;
}

export function extractJsonObject(text: string): unknown {
  const fenced = text.match(/```(?:json)?\s*([\s\S]*?)```/i);
  const candidate = fenced?.[1] ?? text;
  const start = candidate.indexOf("{");
  const end = candidate.lastIndexOf("}");
  if (start < 0 || end < start) {
    throw new Error("No JSON object was found in the Copilot response.");
  }

  return JSON.parse(candidate.slice(start, end + 1));
}

function required(value: unknown, field: string, errors: string[]): void {
  if (typeof value !== "string" || value.trim().length === 0) {
    errors.push(`${field} is required.`);
  }
}