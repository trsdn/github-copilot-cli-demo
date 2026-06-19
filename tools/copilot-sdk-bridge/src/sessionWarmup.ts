import { performance } from "node:perf_hooks";
import { loadCopilotSdkRuntime } from "./copilotSdkRuntime.js";

export type BridgeStatus = {
  ready: boolean;
  mockMode: boolean;
  sessionId?: string;
  model: string;
  models: string[];
  lastError?: string;
  warmupMs?: number;
};

export class CopilotRuntime {
  private client?: any;
  private session?: any;
  private models: Array<{ id: string }> = [];
  private lastError?: string;
  private warmupMs?: number;
  private model = "default";

  get activeSession(): any | undefined {
    return this.session;
  }

  get activeModel(): string {
    return this.model;
  }

  status(): BridgeStatus {
    return {
      ready: process.env.COPILOT_BRIDGE_MOCK === "1" || Boolean(this.session),
      mockMode: process.env.COPILOT_BRIDGE_MOCK === "1",
      sessionId: this.session?.sessionId,
      model: this.model,
      models: this.models.map(model => model.id),
      lastError: this.lastError,
      warmupMs: this.warmupMs,
    };
  }

  async warmup(preferredModel?: string): Promise<BridgeStatus> {
    const start = performance.now();
    this.lastError = undefined;

    if (process.env.COPILOT_BRIDGE_MOCK === "1") {
      this.model = "mock";
      this.warmupMs = performance.now() - start;
      return this.status();
    }

    try {
      const { CopilotClient, approveAll } = await loadCopilotSdkRuntime();
      this.client ??= new CopilotClient({ logLevel: "error", cwd: process.cwd() });
      await this.client.start();
      this.models = await this.client.listModels().catch(() => []);
      this.model = chooseModel(this.models, preferredModel);
      this.session ??= await this.client.createSession({
        clientName: "invoice-dropper-demo",
        model: this.model === "default" ? undefined : this.model,
        onPermissionRequest: approveAll,
        systemMessage: {
          mode: "append",
          content: "You extract fields from fictional sample invoices for a local demo. Return strict JSON only when asked.",
        },
        availableTools: [],
        infiniteSessions: { enabled: false },
      });

      this.warmupMs = performance.now() - start;
      return this.status();
    } catch (error) {
      this.lastError = error instanceof Error ? error.message : String(error);
      this.warmupMs = performance.now() - start;
      return this.status();
    }
  }

  async shutdown(): Promise<void> {
    await this.session?.disconnect().catch(() => undefined);
    if (this.client) {
      await this.client.stop().catch(() => []);
    }

    this.session = undefined;
    this.client = undefined;
  }
}

function chooseModel(models: Array<{ id: string }>, preferredModel?: string): string {
  if (preferredModel && preferredModel !== "default" && models.some(model => model.id === preferredModel)) {
    return preferredModel;
  }

  const preferred = ["claude-sonnet-4.5", "claude-sonnet-4", "gpt-5", "gpt-4.1"];
  return preferred.find(candidate => models.some(model => model.id === candidate)) ?? models[0]?.id ?? "default";
}