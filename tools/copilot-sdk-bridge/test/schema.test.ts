import assert from "node:assert/strict";
import test from "node:test";
import { readFixture } from "../src/fixtures.js";
import { validateInvoice } from "../src/schema.js";

test("fixture validates", async () => {
  const fixture = await readFixture("contoso-office-supplies.invoice.json");
  assert.deepEqual(validateInvoice(fixture), []);
});