# MongoDB safety

- Never log connection strings, credentials, resume tokens, or full command BSON by default.
- Prefer singleton `IMongoClient` per unique cluster settings; do not recreate clients per request.
- Do not register indexes or conventions as side effects during ordinary collection resolution.
- Telemetry must stay opt-in, exporter-neutral, redacted, and low-cardinality (M6; not shipped in core).
- Integration tests use Testcontainers.MongoDb and require a running Docker daemon.
- Known correctness defects remain inventoried in [docs/product/v1-defects.md](../../docs/product/v1-defects.md) (D14–D16, D23–D26) until #18; do not silently widen behavior without tests.
