import { createRequire } from "node:module";
import { loadCopilotSdkRuntime } from "./copilotSdkRuntime.js";
import { CopilotRuntime } from "./sessionWarmup.js";

export async function runPreflight() {
  const checks: Array<{ name: string; ok: boolean; detail: string }> = [];
  const require = createRequire(import.meta.url);

  try {
    const packagePath = require.resolve("@github/copilot/package.json");
    checks.push({ name: "@github/copilot package", ok: true, detail: packagePath });
  } catch (error) {
    checks.push({ name: "@github/copilot package", ok: false, detail: error instanceof Error ? error.message : String(error) });
  }

  try {
    const sdk = await loadCopilotSdkRuntime();
    checks.push({ name: "@github/copilot copilot-sdk runtime", ok: Boolean(sdk.CopilotClient), detail: "CopilotClient runtime resolved" });
  } catch (error) {
    checks.push({ name: "@github/copilot copilot-sdk runtime", ok: false, detail: error instanceof Error ? error.message : String(error) });
  }

  const runtime = new CopilotRuntime();
  const status = await runtime.warmup();
  checks.push({
    name: "SDK warmup",
    ok: status.ready,
    detail: status.ready ? `ready model=${status.model}` : status.lastError ?? "not ready",
  });
  await runtime.shutdown();

  return checks;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  const checks = await runPreflight();
  for (const check of checks) {
    console.log(`${check.ok ? "OK" : "WARN"} ${check.name}: ${check.detail}`);
  }

  process.exitCode = checks.some(check => !check.ok) ? 1 : 0;
}