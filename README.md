# Copilot CLI Demo: Upgrade Regex Extraction To Copilot SDK

This repo is a 45-minute demo package. The app is intentionally small: it is the thing you improve on stage so the audience sees Copilot CLI, planning, grilling, agent work, validation, and the GitHub Copilot SDK upgrade path.

The demo story is:

```text
Existing regex/JSON extractor -> Copilot CLI enhancement workflow -> native app feature backed by GitHub Copilot SDK
```

## What To Show

- Start with `Legacy regex baseline` in the WinUI app.
- Show why the implementation is brittle.
- Use Copilot CLI to plan and grill the enhancement.
- Switch to `Copilot SDK live` to show the app integration path.
- Keep `Deterministic fixture mode` as the reliable recovery lane.

## Quick Start

```powershell
npm run demo:sdk-install
$env:COPILOT_BRIDGE_MOCK = "1"
npm run demo:sdk-bridge
npm run demo:app
```

Open `demo-visuals/index.html` for the concept leave-behind.

## Validation

```powershell
dotnet test tests/InvoiceDropper.Core.Tests/InvoiceDropper.Core.Tests.csproj
npm test --prefix tools/copilot-sdk-bridge
dotnet build InvoiceDropperDemo.sln
```

All invoice data is fictional.