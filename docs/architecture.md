# Architecture and design decisions

## Why a single ASP.NET Core MVC project?

PADO ContactFlow deliberately stays a **small vertical MVC application**:

- `Controllers/ContactsController.cs`: HTTP endpoints, antiforgery, actionable HTTP responses.
- `Models/ContactFieldsRequest.cs`: shared creation/editing validation and mapping.
- `Services/CsvImportService.cs`: UTF-8 CSV parsing, headers, field rules, physical-line diagnostics, in-file duplicates.
- `Data/Repositories/ContactRepository.cs`: Dapper and exclusively parameterized SQL; SQL Server transactions.
- `ViewComponents/ContactListViewComponent.cs`: paginated list rendered by Razor.
- `Views/`: accessible form/table, search, CSV error table, editing and confirmation flow.
- `wwwroot/js/contacts.js`: small progressive enhancement layer, not a client-side framework.

**No EF Core, CQRS bus, generic repository, microservices, or artificial service layer.**

## Phone uniqueness

Normalized user inputs trim leading/trailing whitespace. `dbo.Contacts.Phone` is unique
via `Database/003_UniquePhone.sql` (SQL collation determines case sensitivity).
CSV detects in-file duplicates with case-insensitive comparison before writing. The import
controller performs an existing-phone preflight, but this is not a concurrency guarantee:
SQL Server's unique index is the ultimate guard against concurrent writes. If the index
rejects a race, the import transaction rolls back and a friendly conflict appears.

We intentionally do **not** strip `+`, spaces, hyphens or country codes inside the phone:
doing so correctly requires a regional phone-number normalization library and product rules.

## CSV transaction contract: all or nothing

1. Verify extension/size (5 MB) at HTTP boundary.
2. Read **strict UTF-8**, with optional UTF-8 BOM. Header names match case-insensitively;
   five exact columns are required, arbitrary order supported. CSV follows RFC-style quoting.
3. Validate every row (10,000 row cap): required Name/Phone, `DATE` format, nonfuture DOB,
   boolean, and salary `DECIMAL(18,2)`. Reject duplicates by Phone within file.
4. Check phones already in database. If any invalid row/duplicate, persist **zero** rows.
5. Insert all validated contacts inside **one SQL transaction**. Roll back on any exception;
   DB unique index resolves race conflicts. Report imported count only after commit.

CSV salary accepts `.` as the decimal separator or a quoted Ukrainian decimal comma. Fields containing commas must be quoted.
Errors refer to CSV physical line numbers, including header at line 1. Embedded newlines
inside quoted fields may shift the reported line to the end of the record.

## Search, pagination, and injection defenses

The search and column filter run in SQL Server over *all* contacts, before `COUNT` and `OFFSET`.
Only mapped enum values can form SQL identifiers and CASE expressions. Search patterns
are bound as `@Pattern`, wildcards escaped, page and page-size bound numerically.
Server sorting applies deterministic `Id DESC` tie-breakers. Client quick tools affect
**only** current-page rows and are labeled as such.

## Error and security boundaries

POST, PUT, DELETE are antiforgery-protected. JSON failures return generic user messages;
exceptions are written to structured `ILogger` logs without dumping CSV row contents.
Razor escapes stored contact values; JavaScript updates them through `textContent`.
There is no authentication. The app is a **local demonstration tool**, not an internet-facing
multi-tenant SaaS. Deploying publicly requires authentication, authorization and HTTPS.
