import assert from "node:assert/strict";
import test from "node:test";
import { createBridgeServer } from "../src/server.js";

test("health and mock extraction endpoints work", async () => {
  process.env.COPILOT_BRIDGE_MOCK = "1";
  const server = createBridgeServer();
  await new Promise<void>(resolve => server.listen(0, "127.0.0.1", resolve));
  const address = server.address();
  assert.equal(typeof address, "object");
  const port = typeof address === "object" && address ? address.port : 0;

  try {
    const health = await fetch(`http://127.0.0.1:${port}/health`).then(response => response.json());
    assert.equal(health.ok, true);

    const warmup = await fetch(`http://127.0.0.1:${port}/warmup`, { method: "POST", body: "{}" }).then(response => response.json());
    assert.equal(warmup.ready, true);

    const extracted = await fetch(`http://127.0.0.1:${port}/extract`, {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ sourceFileName: "contoso-office-supplies.txt", contentText: "invoice" }),
    }).then(response => response.json());
    assert.equal(extracted.invoiceNumber, "INV-2026-0619");
  } finally {
    server.close();
    delete process.env.COPILOT_BRIDGE_MOCK;
  }
});