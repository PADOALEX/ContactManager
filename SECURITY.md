# Security policy

## Supported scope

PADO ContactFlow is intended for **local portfolio demonstrations**. It has no login or per-user authorization. Do not deploy an internet-facing, publicly writable instance with real contact data without adding authentication, authorization, secure hosting and privacy controls.

## Dependency security

NuGet audits are enabled and package vulnerability warnings are treated as errors. Dependabot checks the dependency graph weekly. The patched PKCS dependency is pinned to avoid restoring vulnerable transitive versions.

## Reporting a vulnerability

Please report security issues **privately** using GitHub's repository security advisory reporting feature (if available), rather than publishing exploits or sensitive data in a public issue. Do not include personal contact records, database credentials or real CSV data in reports.

## Configuration

Store credentials in environment variables or .NET user-secrets, never committed appsettings files. `TrustServerCertificate=True` and Docker's `sa` example are for **local development only**.
