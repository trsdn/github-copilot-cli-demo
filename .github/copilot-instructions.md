# Copilot instructions

A 45-minute demo package for GitHub Copilot CLI and the GitHub Copilot SDK. A native
WinUI 3 app extracts structured fields from synthetic invoices and copies the result to
the clipboard. All invoice data is fictional.

**This branch (`live-build-start`) is the demo's starting point: the Copilot SDK provider
has been deliberately removed.** The app ships with only the brittle regex baseline and a
deterministic fixture fallback. Rebuilding the SDK extraction path live — with Copilot CLI
driving the work — is the point of the demo. The Node SDK bridge under
`tools/copilot-sdk-bridge` is preserved as the wiring target the live build connects to.

## Architecture

The target runtime (what the live build reconstructs) is a chain that crosses three
language boundaries:

```
WinUI app -> C# provider -> local HTTP SDK bridge -> @github/copilot SDK -> Copilot session -> JSON -> clipboard
```

Today the C# `CopilotSdkInvoiceExtractionProvider` and its `Copilot SDK live` combo entry
are absent — they get reintroduced on stage.

- **`src/InvoiceDropper.Core`** (net8.0 class library) — provider abstraction
  (`IInvoiceExtractionProvider`), the `InvoiceExtractionResult` record, and
  `InvoiceExtractionSchema` validation. No UI or WinUI dependencies.
- **`src/InvoiceDropper.WinUI`** (net8.0-windows, WinUI 3 / Windows App SDK) — drag-and-drop
  UI. `MainPage.xaml.cs` is the composition root: it builds the providers and selects
  between them via `ProviderCombo.SelectedIndex` (0 = legacy regex baseline, 1 = mock
  fixture; index 0 maps to the regex provider, anything else falls through to mock). The
  live build adds a third entry for the SDK provider.
- **`tools/copilot-sdk-bridge`** (Node, ESM, TypeScript via `tsx`) — local HTTP server on
  `127.0.0.1:48731` that wraps the `@github/copilot` SDK. Endpoints: `/health`, `/status`,
  `/preflight`, `/warmup`, `/extract`, `/shutdown`.
- **`sample-data/invoices`** — deterministic fixtures. `*.invoice.json` files are the
  canonical extraction results; `.txt`/`.html` are the human-facing source documents.

### Extraction providers (current vs. live-build target)

- `LegacyRegexInvoiceExtractionProvider` ("Legacy regex baseline", combo index 0) is the
  intentionally brittle starting feature. Only the perfectly-shaped
  `contoso-office-supplies.txt` matches cleanly; other files yield blank/best-effort
  results. This is what the demo improves.
- `MockInvoiceExtractionProvider` ("Deterministic fixture mode", combo index 1) reads a
  `*.invoice.json` fixture directly. Always works offline; the reliable recovery lane.
- `CopilotSdkInvoiceExtractionProvider` ("Copilot SDK live") is **removed on this branch**
  and rebuilt live. When reintroduced it POSTs to the bridge's `/extract` and should
  **silently fall back to the mock provider on any exception or 10s timeout** — preserve
  that resilience so the demo never hard-fails in front of an audience.
- The bridge mirrors this: when `COPILOT_BRIDGE_MOCK=1` or no SDK session is warmed, it
  returns fixture data instead of calling the live model.

## Build, test, run

Run from the repo root (PowerShell).

```powershell
npm run demo:sdk-install                         # install bridge deps (tools/copilot-sdk-bridge)
dotnet build InvoiceDropperDemo.sln              # build C# solution
npm test                                         # full suite: dotnet tests + bridge tests
dotnet test tests/InvoiceDropper.Core.Tests/InvoiceDropper.Core.Tests.csproj   # C# tests only
dotnet test --filter FullyQualifiedName~MockInvoiceExtractionProviderTests     # single C# test class
npm --prefix tools/copilot-sdk-bridge test       # bridge tests only (node --test)
npm --prefix tools/copilot-sdk-bridge test -- test/schema.test.ts              # single bridge test file
pwsh scripts/preflight.ps1                        # environment / readiness check
```

Run the bridge and app (separate terminals):

```powershell
$env:COPILOT_BRIDGE_MOCK="1"; npm run demo:sdk-bridge   # deterministic bridge for rehearsal
npm run demo:app                                         # dotnet run the WinUI app
```

The bridge uses the Node built-in test runner (`node --import tsx --test test/*.test.ts`),
not Jest/Vitest. C# tests are the only dotnet test project.

## Conventions

- **Validation logic is duplicated on purpose** in two languages and must stay in sync:
  `InvoiceExtractionSchema.Validate` (C#) and `validateInvoice` (`schema.ts`). Both enforce
  the same rules: required fields, `subtotal == sum(lineItems.amount)`,
  `total == subtotal + tax + shipping` (0.01 tolerance), `0 <= confidence <= 1`. The C#
  side additionally requires `dueDate >= invoiceDate`. Change one, change the other.
- **JSON is camelCase across the wire.** The extraction prompt (`buildPrompt` in
  `extractInvoice.ts`) lists the exact camelCase field order; C# serialization options on
  `InvoiceExtractionResult` map to the same shape. Keep field names aligned with the
  `InvoiceExtractionResult` type/record in both `schema.ts` and Core.
- **Fixture naming:** an extraction for `foo.txt` resolves to `foo.invoice.json`. Both
  `MockInvoiceExtractionProvider.ResolveFixturePath` and `fixtures.ts#readFixture` strip
  the extension, append `.invoice.json`, and fall back to the first/Contoso fixture if
  missing. Adding a sample means adding the matching `*.invoice.json`.
- The bridge port `48731` is hardcoded in the C# providers (`MainPage`, the SDK provider)
  and defaulted in `server.ts` via `COPILOT_BRIDGE_PORT`. Change all sites together.
- Core targets plain `net8.0` with nullable + implicit usings enabled and has **no** WinUI
  reference — keep extraction/validation logic there, not in the WinUI project.
- The SDK session is configured (`sessionWarmup.ts`) with `availableTools: []` and an
  append system message; model selection prefers `claude-sonnet-4.5` then falls back. Live
  calls are bounded to 10s (`sendAndWait(..., 10000)`).

## Running it live (presentation context)

This package is presented live in front of an audience. The owner has been burned by
scripts and tooling that flash or pop up visible PowerShell/console windows mid-demo.

- Do **not** add steps, scripts, scheduled tasks, or watchers that spawn visible console
  windows. Run background work silently (e.g. async/detached, `-WindowStyle Hidden`, or a
  hidden launcher) and keep output in the current terminal.
- Long-running pieces (the SDK bridge, app) are started intentionally by the presenter in
  known terminals — don't auto-launch extra windows on their behalf.
- Favor deterministic/offline paths during rehearsal (`COPILOT_BRIDGE_MOCK=1`, mock
  provider) so a demo never blocks on network, auth, or model warm-up.

## Demo material (not code)

`prompts/`, `docs/` (runbook, rehearsal checklist, safety-and-yolo-mode), and
`demo-visuals/index.html` are presentation assets. Treat them as documentation; update the
runbook if you change commands or flow.
