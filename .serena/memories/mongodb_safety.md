# MongoDB safety

- Never log connection strings, credentials, resume tokens, or full command BSON by default.
- Prefer singleton `IMongoClient` per unique cluster settings; do not recreate clients per request.
- Do not register indexes or conventions as side effects during ordinary collection resolution.
- Telemetry must stay opt-in, exporter-neutral, redacted, and low-cardinality (M6; not shipped in core).
- Integration tests use Testcontainers.MongoDb and require a running Docker daemon.
- Known correctness defects D14–D16 and D23–D26 are **resolved in M3**; remaining streaming redesign is M4. Do not silently widen Result/streaming/ETag/soft-delete behavior without tests.
