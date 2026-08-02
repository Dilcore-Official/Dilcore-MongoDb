# MongoDB safety

- Never log connection strings, credentials, resume tokens, or full command BSON by default.
- Integration tests use Testcontainers.MongoDb; require a running Docker daemon.
- Prefer singleton IMongoClient per unique cluster settings; do not recreate clients per request.
- Known v1 correctness defects (soft-delete, ETag, pre-success mutation, `$set` update path, bulk edge cases, Result/streaming errors) are inventoried as D14–D17 and D23–D26 in docs/product/v1-defects.md — fix in #18; do not silently widen behavior without tests.
- Do not register indexes/conventions as side effects during ordinary collection resolution in v2 design.
- Telemetry must be opt-in, exporter-neutral, redacted, low-cardinality (M6).
