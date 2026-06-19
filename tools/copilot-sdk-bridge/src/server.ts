import http from "node:http";
import { URL } from "node:url";
import { extractInvoiceWithSdk } from "./extractInvoice.js";
import { runPreflight } from "./preflight.js";
import { validateInvoice, type ExtractRequest } from "./schema.js";
import { CopilotRuntime } from "./sessionWarmup.js";

const runtime = new CopilotRuntime();
const port = Number(process.env.COPILOT_BRIDGE_PORT ?? 48731);

export function createBridgeServer() {
  return http.createServer(async (request, response) => {
    try {
      const url = new URL(request.url ?? "/", `http://${request.headers.host ?? "localhost"}`);
      if (request.method === "GET" && url.pathname === "/health") {
        return sendJson(response, 200, { ok: true, package: "@github/copilot", mockMode: process.env.COPILOT_BRIDGE_MOCK === "1" });
      }

      if (request.method === "GET" && url.pathname === "/status") {
        return sendJson(response, 200, runtime.status());
      }

      if (request.method === "GET" && url.pathname === "/preflight") {
        return sendJson(response, 200, { checks: await runPreflight() });
      }

      if (request.method === "POST" && url.pathname === "/warmup") {
        const body = await readJson<{ preferredModel?: string }>(request).catch(() => ({}));
        return sendJson(response, 200, await runtime.warmup(body.preferredModel));
      }

      if (request.method === "POST" && url.pathname === "/extract") {
        const body = await readJson<ExtractRequest>(request);
        const result = await extractInvoiceWithSdk(runtime.activeSession, body, runtime.activeModel);
        const errors = validateInvoice(result);
        if (errors.length > 0) {
          return sendJson(response, 422, { error: "validation_failed", errors });
        }

        return sendJson(response, 200, result);
      }

      if (request.method === "POST" && url.pathname === "/shutdown") {
        await runtime.shutdown();
        return sendJson(response, 200, { ok: true });
      }

      return sendJson(response, 404, { error: "not_found" });
    } catch (error) {
      return sendJson(response, 500, { error: error instanceof Error ? error.message : String(error) });
    }
  });
}

function sendJson(response: http.ServerResponse, statusCode: number, body: unknown) {
  response.writeHead(statusCode, { "content-type": "application/json; charset=utf-8" });
  response.end(JSON.stringify(body, null, 2));
}

async function readJson<T>(request: http.IncomingMessage): Promise<T> {
  const chunks: Buffer[] = [];
  for await (const chunk of request) {
    chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
  }

  const text = Buffer.concat(chunks).toString("utf8");
  return text ? JSON.parse(text) as T : {} as T;
}

if (import.meta.url === `file://${process.argv[1]}`) {
  createBridgeServer().listen(port, "127.0.0.1", () => {
    console.log(`Copilot SDK bridge listening on http://127.0.0.1:${port}`);
  });
}