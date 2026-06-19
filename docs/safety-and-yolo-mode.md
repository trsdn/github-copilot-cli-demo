# Safety And Allow-All Mode

Allow-all mode is a speed lever for demos because it removes approval friction. It is not the recommended default for primary machines or production workflows.

Recommended production pattern:

1. Use Remote SSH to a separate, disposable machine.
2. Run agent work inside a Docker dev container.
3. Restrict writable paths and commands with a documented policy.
4. Keep invoice and customer data synthetic or sanitized.
5. Log SDK bridge actions without logging sensitive invoice contents.

Blast-radius controls already in this repo:

- The SDK bridge binds only to `127.0.0.1:48731` (localhost).
- All sample invoices are fictional.
- Deterministic mock fallback is available at all times.
- No sensitive invoice contents are logged.
- No extra console windows are spawned during the demo.

This demo runs locally for practicality, not because local allow-all is the recommended posture.