# 45-Minute Demo Runbook

## Demo Thesis

Do not spend the session building a whole app from scratch. The app is just the demo object. The real demo is upgrading a brittle regex/JSON extraction feature into a Copilot SDK-backed AI feature using Copilot CLI as the development cockpit.

## 0-4 min: Open With The Existing Feature

Run the WinUI app and select `Legacy regex baseline`. Load `contoso-office-supplies.txt`. Show that it works only because the text is perfectly shaped.

Say: "This is the kind of feature teams already have: regex, JSON glue, and a pile of edge cases waiting to happen."

## 4-8 min: Show The Leave-Behind

Open `demo-visuals/index.html`. Point to the upgrade arc: baseline, Copilot CLI work loop, SDK bridge, validation, fallback.

## 8-15 min: Use Copilot CLI To Plan And Grill

Use the prompts in `prompts/`. Reference the `grill-me` skill as the interrogation step: SDK auth, bad invoice formats, PDF reliability, latency, and demo fallback.

Keep this brisk. The audience should see Copilot CLI helping shape engineering judgment, not replacing it.

## 15-25 min: Show The Enhancement Diff

Walk through the provider abstraction and the local SDK bridge. The key point is that the app now has a swappable extraction strategy: regex baseline, deterministic fixture, SDK live.

## 25-32 min: Run The SDK Path

Start the bridge. Use mock mode for rehearsal unless live SDK readiness is already confirmed.

```powershell
$env:COPILOT_BRIDGE_MOCK = "1"
npm run demo:sdk-bridge
```

Switch the app to `Copilot SDK live`, warm it up, and load the sample invoice.

## 32-36 min: Clipboard Proof

Copy the result and paste it into VS Code or Notepad. The proof is the workflow: unstructured invoice text becomes structured output the user can move into another system.

## 36-41 min: Safety And YOLO Framing

Explain allow-all mode as a demo speed lever. The recommended production pattern is Remote SSH plus dev container plus explicit policy boundaries.

## 41-45 min: Close With The Reusable Pattern

Summarize the reusable path: identify brittle automation, isolate it behind an interface, use Copilot CLI to plan and harden the change, use Copilot SDK for the app feature, validate with tests, keep fallback for the live demo.