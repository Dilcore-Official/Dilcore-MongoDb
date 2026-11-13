# Production MongoDB security guidance

Current operational guidance for hosts that consume Dilcore MongoDB. This is **not** a substitute for MongoDB server hardening or for [SECURITY.md](../../SECURITY.md) vulnerability reports.

## Configuration and secrets

- Load connection strings and credentials from the host secret store (environment variables, Azure Key Vault, AWS Secrets Manager, Kubernetes secrets). Do not commit them, and do not put them in sample code.
- Prefer `MongoClientSettings` constructed in the host over embedding passwords in source. TLS, authentication mechanism, and certificate validation belong on driver settings.
- Dilcore registers one singleton `IMongoClient` per unique cluster key. Do not construct additional clients per request.

## TLS and the driver

- Enable TLS through `MongoClientSettings` / connection-string options (`tls=true`). Do not disable certificate validation in production.
- Timeouts, retry writes, compression, and `applicationName` are driver settings; pass them on the cluster registration. Dilcore does not wrap those knobs.

## Redaction

- Never log connection strings, credentials, or change-stream resume tokens.
- Prefer logging cluster **keys** (the names you registered) rather than hosts or connection strings.
- Treat command BSON dumps as sensitive; do not enable them by default.

## Least privilege

- Application users should have only the roles they need (`readWrite` on application databases, not `root`).
- Provisioning (`IMongoDbProvisioner`) needs index/collection create privileges. Run it as a deployment step, not with a request-path identity that is otherwise read-only.
- Unique indexes and schema validation belong in provisioning, not in ad-hoc request code.

## Tenant namespace isolation

- Dilcore has no first-class tenant type. Apps own `INamespacePrefixResolver` / `INamespaceSegmentContributor`.
- Missing prefixes **fail closed** at resolution. Do not fall back to a shared collection when a tenant prefix is absent.
- JSON adapters and typed repositories resolve the same physical collection through `IMongoDbCollectionFactory`. Keep named bindings distinct per tenant-sensitive collection.

## JSON adapters

- `Dilcore.MongoDB.NewtonsoftJson` rejects `TypeNameHandling` other than `None` for untrusted input.
- Prefer Canonical Extended JSON when BSON type fidelity matters. Relaxed JSON cannot preserve every BSON distinction.

## Transactions

- Client-side byte budgets are estimates. MongoDB’s 16 MiB limit is per BSON document / oplog entry, not a total-transaction cap.
- Transaction callbacks can run more than once on transient errors. Do not perform non-idempotent external side effects inside `WithTransactionAsync`.
