# PADO ContactFlow — Code review summary

## Baseline inspected

The supplied `ContactManager-main(6).zip` included one ASP.NET Core MVC / .NET 10 project, a Dapper SQL Server repository, CSVHelper import, a Razor ViewComponent for listing, an inline-edit JavaScript file, four CSV samples and SQL scripts. Build artifacts (`bin/`, `obj/`) were present in the source archive and are deliberately excluded here.

## Issues identified and corrections

| Area | Finding in original | Correction |
|---|---|---|
| CRUD | No manual create action or UI | `Create` endpoint + Bootstrap create modal; shared validated request |
| Search | Current-page browser filter only | Server-side Dapper search before page selection; current-page tools retained and labeled |
| CSV encoding | Implicit default StreamReader decoding | Strict UTF-8, optional BOM, invalid bytes rejected |
| CSV schema | Missing headers checked but extra/variant headers not strictly handled | Exact 5-column schema, any order, case-insensitive, no duplicates/extra columns |
| CSV diagnostics | Basic per-row field errors only | Physical line numbers, malformed field count/quoting, cross-row phone duplicates, checked-row summary |
| DB duplicates | No unique phone constraint | Idempotent schema migration with duplicate-data precheck and unique index |
| Concurrency | Batch insert had a transaction, no duplicate preflight | Preflight for friendly errors, unique index for race protection, rollback on failure |
| SQL safety | Existing parameter binding and sort enum were correct | Preserved; added LIKE escaping, server filtering and cancellation propagation |
| UI | Inline editing and native deletion confirmation existed | Added create form, feedback, import busy state, CSV error table, responsive tweaks |
| Form validation | Update had inline salary/DOB checks | Common DataAnnotations/IValidatableObject validation for create and update |
| Security | Some write endpoints had antiforgery | Retained and added global auto-validation for unsafe methods |
| Tests | No test project | xUnit CSV/validation, WebApplicationFactory smoke tests, real SQL rollback tests |
| Reproducibility | Only manual LocalDB setup | LocalDB instructions, Docker Compose, SQL scripts, example settings, CI |

## Deliberately excluded

No EF Core replacement, separate REST API, authentication/authorization subsystem, external cache, frontend build pipeline, or multi-layer project split. An unauthenticated local demo should **not** be exposed to the public internet.

## Verification and security status

Windows PowerShell restore and Release build succeeded on the original upgrade (the then-resolved PKCS dependency emitted two vulnerability warnings). The local non-SQL test run reported **23 passed, 0 failed**.

Both GitHub Actions jobs succeeded for the baseline migration and for the security-updated revision: Linux Release build/unit/MVC tests and SQL Server 2022 integration tests. [Verified main-branch CI run](https://github.com/PADOALEX/ContactManager/actions/runs/39124553339).

Final follow-up (October 18, 2026): 29/29 unit/MVC tests and 1/1 SQL Server integration test passed on main; JavaScript syntax check passed. Money is now sent from the browser as decimal text and parsed exactly into .NET decimal; the view component forwards request cancellation. Dapper and test runner dependencies and both GitHub Actions jobs are refreshed. The final dependency revision uses Microsoft.Data.SqlClient 7.1.1 plus pinned patched System.Security.Cryptography.Pkcs 10.0.13. NuGet warnings NU1901–NU1904 are configured as errors, and Dependabot checks for updates automatically.

A manual Windows LocalDB browser acceptance test was **not independently observed**. This is a local demonstration application without user authentication or per-user authorization, not a production-ready multi-tenant service.
