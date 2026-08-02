# MongoDB safety

- Never log connection strings, credentials, resume tokens, or full command BSON by default.
- Integration tests use Testcontainers.MongoDb; require a running Docker daemon.
- Prefer singleton IMongoClient per unique cluster settings; do not recreate clients per request.
- Soft-delete, ETag, and Result-based error handling have known v1 defects (see docs/product/v1-defects.md) — fix in #18, do not silently widen behavior without tests.
- Do not register indexes/conventions as side effects during ordinary collection resolution in v2 design.
- Telemetry must be opt-in, exporter-neutral, redacted, low-cardinality (M6).
