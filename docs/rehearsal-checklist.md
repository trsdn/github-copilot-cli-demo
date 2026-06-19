# Rehearsal Checklist

## Components

- Run `pwsh scripts/preflight.ps1` and document any warning.
- Run `dotnet test tests/InvoiceDropper.Core.Tests/InvoiceDropper.Core.Tests.csproj`.
- Run `npm --prefix tools/copilot-sdk-bridge test`.
- Build `InvoiceDropperDemo.sln`.
- Start the bridge in mock mode (`COPILOT_BRIDGE_MOCK=1`) and verify endpoints:
  - `Invoke-RestMethod http://127.0.0.1:48731/health`
  - `Invoke-RestMethod -Method Post http://127.0.0.1:48731/warmup -Body '{"preferredModel":"default"}' -ContentType application/json`
  - `Invoke-RestMethod -Method Post http://127.0.0.1:48731/extract -Body '{"sourceFileName":"contoso-office-supplies.txt"}' -ContentType application/json`

## Flow

- Mock mode: drag every sample invoice and verify extracted fields.
- Clipboard: copy output and paste into Notepad or VS Code.
- Legacy baseline: load the sample and show the regex parser working only on the perfectly shaped invoice.
- Failure path: stop the bridge and confirm the app falls back to deterministic mode.

## Timed checkpoints (under 45 minutes)

- 8 min: leave-behind done, CLI planning started.
- 15 min: plan grilled, enhancement underway.
- 25 min: enhancement diff shown.
- 32 min: SDK path run, or fallback decision made.
- 41 min: safety recap done, heading into close.