# Architecture

.NET 10 MongoDB application toolkit (v1 branded DocumentDb; v2 renames to Dilcore.MongoDB).

## Current v1 packages
- Dilcore.DocumentDb.Abstractions — entity/prefix contracts (FluentResults)
- Dilcore.DocumentDb.MongoDb.Abstractions — Mongo interfaces/options; RootNamespace incorrectly equals Dilcore.DocumentDb.Abstractions
- Dilcore.DocumentDb.MongoDb — DI entry AddMongoDb, providers, config
- Dilcore.DocumentDb.MongoDb.Repositories — IGenericRepository / bulk / projection

## Graph
Abstractions <- MongoDb.Abstractions <- MongoDb <- Repositories

## Entry point
ServiceCollectionExtensions.AddMongoDb(configure, container => container.AddDatabase(...))

## v2 decision (M0)
Two packages: Dilcore.MongoDB.Abstractions + Dilcore.MongoDB; hard rename; no shims. See docs/adr/0001-package-naming.md and ROADMAP.md.
