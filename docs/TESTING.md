# Verification & test report

> Updated 18 October 2026. This document distinguishes **observed CI results** from checks not performed.

## Confirmed results

| Test environment | Check | Observed result |
| :-- | :-- | :-- |
| Windows PowerShell (.NET 10.0.10) | `dotnet restore` | Passed; older dependency produced NU1902/NU1903 before the security patch |
| Windows PowerShell | `dotnet build -c Release` | Passed; older dependency produced NU1902/NU1903 before the patch |
| Windows PowerShell | Unit + MVC tests excluding SQL | **23 passed, 0 failed, 0 skipped** |
| GitHub Actions, Ubuntu | Restore, Release build, unit/MVC tests | **Succeeded** |
| GitHub Actions, Ubuntu with SQL Server 2022 service | Integration test job | **Succeeded** |
| Windows LocalDB browser CRUD & import | Manual end-to-end run | Not independently observed |

Evidence: [successful GitHub Actions run from 18 October 2026](https://github.com/PADOALEX/ContactManager/actions/runs/39122100044). The CI job summary confirms both jobs completed successfully. The 23-test count is taken from the provided local PowerShell output, not inferred from the CI summary.

**Security follow-up:** The original SqlClient dependency restored a vulnerable Pkcs package. SqlClient is updated and the patched Pkcs 10.0.13 is explicitly pinned. CI is configured to fail when NuGet reports a vulnerability. Fresh CI is required to validate these changes.

## Test coverage

- **CSV parsing:** UTF-8 BOM, non-ASCII data, invalid UTF-8, unexpected headers, order/case flexibility, malformed column count, in-file duplicates, row-level errors, empty file and quoted delimiters.
- **Controllers/MVC:** GET page, JSON contact creation with antiforgery protection, invalid input rejection, duplicate import preflight, atomic import call.
- **Model validation:** Whitespace, missing values, future dates, salary precision, query enum/page normalization.
- **SQL Server integration:** Dapper CRUD, search and escaping, unique-phone database constraint, all-or-nothing rollback and disposable test database.

## Commands

```powershell
dotnet restore PADO.ContactFlow.sln
dotnet build PADO.ContactFlow.sln -c Release --no-restore
dotnet test PADO.ContactFlow.sln -c Release --no-build --filter 'Category!=SqlServer'
```

SQL integration tests require SQL Server with permission to create and drop a **temporary, randomly named test database**. Do not point at a production SQL Server:

```powershell
$env:CONTACTFLOW_TEST_CONNECTION = 'Server=(localdb)\MSSQLLocalDB;Database=master;Integrated Security=True;TrustServerCertificate=True;'
dotnet test PADO.ContactFlow.sln -c Release --filter 'Category=SqlServer'
Remove-Item Env:CONTACTFLOW_TEST_CONNECTION
```

GitHub Actions creates its own SQL Server 2022 service container and supplies the test connection automatically.

## Limitations

- No authenticated users or per-user authorization; not suitable for a publicly writable contact database.
- No visual browser regression tests or performance benchmarks.
- No claim that manual Windows browser testing has been completed.
