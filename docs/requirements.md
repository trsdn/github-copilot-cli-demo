# Requirements — Native Copilot SDK Invoice Demo

Requirements for the development portion of the 45-minute GitHub Copilot CLI / SDK demo.
This is the de-facto spec; the authoritative architecture and conventions live in
[../.github/copilot-instructions.md](../.github/copilot-instructions.md). All invoice data
is fictional.

## 1. Scope

A native Windows utility that extracts structured fields from a synthetic invoice and copies
the result to the clipboard. The runtime crosses three language boundaries:

```text
WinUI app -> C# provider -> local HTTP SDK bridge -> @github/copilot SDK -> Copilot session -> JSON -> clipboard
```

In scope: the WinUI app, the Core extraction library, the Node SDK bridge, deterministic
fixtures, and the validation rules shared across languages.

Out of scope: real invoice OCR, persistence, authentication beyond the SDK's own, any
network call other than the local bridge, and multi-document batch processing.

## 2. Functional requirements

### FR-1 — Drag-and-drop extraction
- The app accepts an invoice file (`.txt` / `.html`) via drag-and-drop and produces an
  `InvoiceExtractionResult`.
- On success, the result summary + JSON is copied to the clipboard
  (`InvoiceExtractionResult.ToClipboardText`).

### FR-2 — Two providers, selectable in the UI
- `MockInvoiceExtractionProvider` ("Deterministic demo mode") reads the matching
  `*.invoice.json` fixture directly and works fully offline.
- `CopilotSdkInvoiceExtractionProvider` ("Copilot SDK live") POSTs to the bridge `/extract`.
- The active provider is chosen via `ProviderCombo.SelectedIndex` (0 = mock, 1 = SDK live)
  in `MainPage.xaml.cs`.

### FR-3 — Silent fallback (must never hard-fail on stage)
- The SDK provider must fall back to the mock provider on **any** exception or after a
  **10-second** timeout, unless the caller's own `CancellationToken` was cancelled.
- The bridge mirrors this: when `COPILOT_BRIDGE_MOCK=1` or no SDK session is warmed, it
  returns fixture data instead of calling the live model.

### FR-4 — Bridge HTTP contract
- The bridge listens on `127.0.0.1:48731` (hardcoded in the C# providers; defaulted via
  `COPILOT_BRIDGE_PORT` in `server.ts`). Change all sites together.
- Endpoints: `GET /health`, `GET /status`, `GET /preflight`, `POST /warmup`,
  `POST /extract`, `POST /shutdown`.
- `/extract` returns `200` with the result, or `422 { error: "validation_failed", errors }`
  when validation fails.

### FR-5 — Extraction schema (camelCase across the wire)
- The result has this exact camelCase field order (per `buildPrompt` and
  `InvoiceExtractionResult`): `invoiceNumber, vendor, customer, invoiceDate, dueDate,
  purchaseOrderNumber, lineItems, subtotal, tax, shipping, total, currency, paymentTerms,
  paymentReference, confidence, provider, model, sourceFileName, elapsed`.
- Field names must stay aligned between `schema.ts` and Core's `InvoiceExtractionResult`.

### FR-6 — Validation parity (two languages, kept in sync)
`InvoiceExtractionSchema.Validate` (C#) and `validateInvoice` (`schema.ts`) must enforce the
same rules:
- Required: `invoiceNumber`, `vendor.name`, `customer.name`, `purchaseOrderNumber`,
  `currency`, `paymentTerms`, `paymentReference`.
- `lineItems` contains at least one item.
- `subtotal == sum(lineItems.amount)` (0.01 tolerance).
- `total == subtotal + tax + shipping` (0.01 tolerance).
- `0 <= confidence <= 1`.
- **C# only, additionally:** `dueDate >= invoiceDate`.

### FR-7 — Fixture resolution
- An extraction for `foo.txt` resolves to `foo.invoice.json`
  (`MockInvoiceExtractionProvider.ResolveFixturePath` and `fixtures.ts#readFixture`).
- Both strip the extension, append `.invoice.json`, and fall back to the first/Contoso
  fixture if the file is missing. Adding a sample means adding the matching
  `*.invoice.json`.

### FR-8 — SDK session configuration
- The session (`sessionWarmup.ts`) is configured with `availableTools: []` and an append
  system message.
- Model selection prefers `claude-sonnet-4.5`, then falls back.
- Live calls are bounded to 10 s (`sendAndWait(..., 10000)`).

## 3. Non-functional requirements

### NFR-1 — Determinism for rehearsal
- A full run must be possible offline using `COPILOT_BRIDGE_MOCK=1` and/or the mock
  provider, with no dependency on network, auth, or model warm-up.

### NFR-2 — No visible console windows mid-demo
- Do not add scripts, scheduled tasks, or watchers that spawn visible PowerShell/console
  windows. Background work runs silently; long-running pieces are started intentionally by
  the presenter in known terminals.

### NFR-3 — Layering
- `InvoiceDropper.Core` targets plain `net8.0` (nullable + implicit usings) with **no** WinUI
  reference. Extraction/validation logic stays in Core, not in the WinUI project.

### NFR-4 — Resilience budget
- End-to-end live extraction is bounded to 10 s before fallback, so the visible UX never
  blocks longer than that.

## 4. Build, test, run (acceptance commands)

```powershell
npm run demo:sdk-install                         # install bridge deps
dotnet build InvoiceDropperDemo.sln              # build C# solution
npm test                                         # dotnet tests + bridge tests
dotnet test tests/InvoiceDropper.Core.Tests/InvoiceDropper.Core.Tests.csproj   # C# tests only
npm --prefix tools/copilot-sdk-bridge test       # bridge tests (node --test)
pwsh scripts/preflight.ps1                        # environment / readiness check
```

Run the bridge + app (separate terminals):

```powershell
$env:COPILOT_BRIDGE_MOCK="1"; npm run demo:sdk-bridge   # deterministic bridge
npm run demo:app                                         # WinUI app
```

## 5. Acceptance criteria

- [ ] Dropping a sample invoice in mock mode copies a valid result to the clipboard offline.
- [ ] Dropping a sample invoice in SDK-live mode returns a bridge result, or silently falls
      back to mock within 10 s on any failure.
- [ ] Identical valid/invalid fixtures produce identical validation outcomes in C# and the
      bridge (except the C# `dueDate >= invoiceDate` rule).
- [ ] `/extract` returns `422` with field errors for an invalid invoice.
- [ ] All commands in section 4 pass.
- [ ] No step opens a visible console window during the demo.
