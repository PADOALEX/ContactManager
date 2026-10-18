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

## Verification caveat

See `docs/TESTING.md`. This authoring environment did not contain a .NET SDK or a running SQL Server. The current implementation is a source-level upgrade **pending SDK build, tests, SQL integration and browser testing**; the documents do not claim otherwise.
