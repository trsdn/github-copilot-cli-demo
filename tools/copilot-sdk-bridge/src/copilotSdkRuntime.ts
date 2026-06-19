import { createRequire } from "node:module";
import path from "node:path";
import { pathToFileURL } from "node:url";

export type RuntimeSdk = {
  CopilotClient: new (options?: Record<string, unknown>) => any;
  approveAll: any;
};

export async function loadCopilotSdkRuntime(): Promise<RuntimeSdk> {
  const require = createRequire(import.meta.url);
  const packagePath = require.resolve("@github/copilot/package.json");
  const sdkPath = path.join(path.dirname(packagePath), "copilot-sdk", "index.js");
  const sdk = await import(pathToFileURL(sdkPath).href) as Partial<RuntimeSdk>;

  if (!sdk.CopilotClient || !sdk.approveAll) {
    throw new Error("The installed @github/copilot package does not expose the expected copilot-sdk runtime files.");
  }

  return { CopilotClient: sdk.CopilotClient, approveAll: sdk.approveAll };
}