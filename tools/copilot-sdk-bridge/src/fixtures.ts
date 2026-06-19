import { readFile } from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import type { InvoiceExtractionResult } from "./schema.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, "..", "..", "..");

export async function readFixture(sourceFileName = "contoso-office-supplies.invoice.json"): Promise<InvoiceExtractionResult> {
  const baseName = path.basename(sourceFileName, path.extname(sourceFileName));
  const normalizedBaseName = baseName.endsWith(".invoice") ? baseName : `${baseName}.invoice`;
  const fixturePath = path.join(repoRoot, "sample-data", "invoices", `${normalizedBaseName}.json`);
  const fallbackPath = path.join(repoRoot, "sample-data", "invoices", "contoso-office-supplies.invoice.json");
  const text = await readFile(fixturePath).catch(() => readFile(fallbackPath));
  return JSON.parse(text.toString()) as InvoiceExtractionResult;
}