# Guides

Task-oriented pages for the Dilcore MongoDB packages. Every page has the same shape: purpose and package, Setup, Use, Handle failures, Pitfalls, See also.

## Choose and set up

- [Choose a package](../product/package-selection.md)
- [Register clusters, databases and document types](register-clusters-and-databases.md)
- [Run MongoDB safely in production](../security/production-mongodb.md)

## Documents

- [Define document types](define-documents.md)
- [Guard updates with an ETag](optimistic-concurrency.md)
- [Soft delete and restore documents](soft-delete.md)

## Read and write

- [Create, read and update documents](crud-with-repositories.md)
- [Page through results with keyset cursors](keyset-paging.md)
- [Write many documents at once](bulk-writes.md)
- [Read only the fields you need](projections.md)
- [Use collections without repositories](resolve-collections.md)
- [Handle typed errors](handle-errors.md)
- [Typed error reference](error-reference.md)

## Tenancy and naming

- [Prefix database and collection names](namespace-prefixes.md)
- [Give each tenant its own database](multi-tenant-databases.md)

## Provisioning

- [Provision collections, indexes and TTL](provision-indexes-and-ttl.md)
- [Expire documents automatically with TTL](expire-documents-with-ttl.md)
- [Add custom provisioning steps](custom-provisioning-steps.md)

## Transactions

- [Commit several writes together in a transaction](transactions.md)

## JSON documents

- [Store JSON documents with System.Text.Json](json-system-text-json.md)
- [Store JSON documents with Newtonsoft.Json](json-newtonsoft.md)
- [Control JSON and BSON conversion](json-conversion-options.md)

## MongoDB driver

- [Reach the MongoDB driver directly](../product/driver-escape-hatches.md)
