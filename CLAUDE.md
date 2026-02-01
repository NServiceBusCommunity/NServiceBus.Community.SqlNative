# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

NServiceBus.Community.SqlServer.Native is a low-level .NET wrapper for the NServiceBus SQL Server Transport that requires no NServiceBus or SQL Server Transport reference. It provides direct access to SQL Server transport queues for scenarios like error/audit queue handling, corrupted message processing, deployment operations, and bulk message operations.

## Build Commands

```bash
# Build the solution
dotnet build src --configuration Release

# Run all tests
dotnet test src --configuration Release --no-build --no-restore

# Run a single test
dotnet test src --configuration Release --filter "FullyQualifiedName~TestClassName.TestMethodName"
```

## Test Requirements

Tests require SQL Server 2019+. The connection string is configured in `src/Shared/Connection.cs`:
- Local development: `Server=.\;Database=NServiceBusNativeTests;Integrated Security=True`
- CI (AppVeyor): `Server=(local)\SQL2019;Database=master;User ID=sa;Password=Password12!`

Create the database before running tests: `CREATE DATABASE NServiceBusNativeTests`

## Architecture

### Core Libraries

**SqlServer.Native** (`src/SqlServer.Native/`) - The main library with no NServiceBus dependency:
- `MainQ/` - Main queue operations: `QueueManager` for CRUD, send, read, consume operations
- `DelayedQ/` - Delayed queue operations: `DelayedQueueManager` for scheduled message handling
- `Dedupe/` - Message deduplication: `DedupeManager`, `DedupeCleanerJob`
- `Subscription/` - Subscription table management: `SubscriptionManager`
- `BaseQ/` - Shared base functionality for queue managers
- `Headers.cs` - Header constants and utilities (copied from NServiceBus.Headers)
- `Serializer.cs` - JSON serialization for message headers
- `ConnectionHelpers.cs` - SQL connection/transaction utilities

**SqlServer.HttpPassthrough** (`src/SqlServer.HttpPassthrough/`) - Bridges HTTP streams to SQL Server transport

**SqlServer.Deduplication** (`src/SqlServer.Deduplication/`) - NServiceBus pipeline behavior for deduplication

### Target Frameworks

- SqlServer.Native: `net48;net9.0` (multi-target for broad compatibility)
- All other projects: `net10.0`

### Documentation Generation

README is auto-generated from `readme.source.md` using MarkdownSnippets. Code snippets are pulled from test files. Run the build to regenerate documentation.

## Code Style

- C# preview language features enabled (`LangVersion=preview`)
- Strict warnings as errors (`TreatWarningsAsErrors=true`)
- Code style enforced in build (`EnforceCodeStyleInBuild=true`)
- See `.editorconfig` for formatting rules (4-space indentation, UTF-8, LF line endings)

## Package Versions

Central package management via `src/Directory.Packages.props`. Key versions:
- Microsoft.Data.SqlClient: 6.1.4
- NServiceBus: 10.0.0
- NServiceBus.Transport.SqlServer: 9.0.0
- xunit.v3: 3.2.2
