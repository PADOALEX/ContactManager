# Verification record

## Environment of this deliverable

- Source-based static audit: completed.
- JavaScript syntax check: `node --check` completed successfully.
- Source package integrity: XML project files, JSON configurations and YAML parsed successfully; project references, renamed namespaces and Razor link attributes statically checked.
- ZIP archive CRC/integrity: passed.
- NuGet restore: **not run** (the authoring environment has no .NET SDK / NuGet network access).
- .NET build: **not run** (SDK unavailable).
- xUnit and WebApplicationFactory tests: **written, not executed locally**.
- SQL Server integration tests: **written, not executed locally** (SQL Server unavailable).
- Browser UI / Windows LocalDB: **not run**.

**No unexecuted test is claimed as passing.** Use the CI workflow or run the commands
from the README locally before representing this as fully verified in a resume.

## Coverage inventory

- CSV unit tests: UTF-8 BOM with Unicode, invalid UTF-8, missing/extra/duplicate headers,
  reordered headers, in-file duplicates, invalid fields and physical line numbers,
  malformed field count, empty file, quoted commas.
- Validation tests: whitespace-only fields, future date, salary precision, normalization,
  safe enum fallback and page-size bounds.
- MVC smoke tests: GET page rendering and POST antiforgery rejection.
- SQL integration: real Dapper CRUD, global search/count, unique constraint,
  atomic transaction rollback, isolated disposable test database.

## Running locally

```powershell
dotnet restore PADO.ContactFlow.sln
dotnet build PADO.ContactFlow.sln -c Release --no-restore
dotnet test PADO.ContactFlow.sln -c Release --no-build --filter 'Category!=SqlServer'
```

Integration tests require an admin connection to SQL Server's `master` database and
**CREATE DATABASE** permission; tests create and delete a **randomly named temporary DB**:

```powershell
$env:CONTACTFLOW_TEST_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;'
dotnet test PADO.ContactFlow.sln -c Release --filter 'Category=SqlServer'
Remove-Item Env:CONTACTFLOW_TEST_CONNECTION
```

For GitHub CI, the SQL integration job uses a throwaway SQL Server container.
