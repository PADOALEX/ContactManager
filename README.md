# Contact Manager

A small ASP.NET Core MVC application built for a Trainee / Junior .NET technical task.

The application imports contacts from CSV files, validates and stores them in Microsoft SQL Server, and provides both server-side and client-side tools for browsing and managing the stored data.

## Features

### CSV import
- Upload `.csv` files from the browser.
- Required columns:
  - `Name`
  - `Date of birth`
  - `Married`
  - `Phone`
  - `Salary`
- Backend processing with CsvHelper.
- File size limit: 5 MB.
- Row-level validation with clear error messages.
- The complete file is validated before database insertion.
- Database insertion is performed in a transaction.

### Contact management
- Display contacts from SQL Server.
- Inline editing directly inside the table.
- Delete contacts with confirmation.
- Server-side validation for updates.
- AJAX update/delete requests protected with an antiforgery token.

### Sorting, filtering, and pagination
- Server-side pagination.
- Server-side ordering by every contact column.
- Client-side filtering with JavaScript.
- Client-side sorting with JavaScript.
- Client-side tools operate on the rows already loaded on the current server page.

### UI architecture
- ASP.NET Core MVC.
- Contact list implemented as a ViewComponent.
- Header and footer implemented as partial views.
- Responsive Bootstrap-based interface with custom styling.

## Tech Stack

| Technology | Purpose |
|---|---|
| .NET 10 / ASP.NET Core MVC | Web application |
| C# | Backend |
| Microsoft SQL Server / LocalDB | Database |
| Dapper 2.1.79 | Data access |
| Microsoft.Data.SqlClient 7.0.2 | SQL Server connection |
| CsvHelper 33.1.0 | CSV parsing |
| JavaScript | Client-side filtering, sorting, inline actions |
| Bootstrap | Responsive UI |

## Project Structure

```text
ContactManager/
├── Database/
│   ├── 001_CreateDatabase.sql
│   └── 002_CreateContactsTable.sql
│
├── Samples/
│   ├── contacts-valid.csv
│   └── contacts-invalid.csv
│
├── src/
│   └── ContactManager.Web/
│       ├── Controllers/
│       ├── Data/
│       │   └── Repositories/
│       ├── Models/
│       ├── Services/
│       ├── ViewComponents/
│       ├── Views/
│       │   ├── Contacts/
│       │   └── Shared/
│       ├── wwwroot/
│       ├── Program.cs
│       ├── appsettings.json
│       └── ContactManager.Web.csproj
│
└── ContactManager.sln
```

## Prerequisites

Install:

- .NET 10 SDK
- Microsoft SQL Server or SQL Server Express LocalDB
- Git

The default project configuration uses:

```text
(localdb)\MSSQLLocalDB
```

with the database:

```text
ContactManagerDb
```

## Database Setup

Connect to your SQL Server instance and execute the scripts in this order:

```text
Database/001_CreateDatabase.sql
Database/002_CreateContactsTable.sql
```

The first script creates `ContactManagerDb` if it does not exist.

The second script creates:

```text
dbo.Contacts
```

with the following schema:

| Column | SQL type |
|---|---|
| Id | `INT IDENTITY` |
| Name | `NVARCHAR(200)` |
| DateOfBirth | `DATE` |
| Married | `BIT` |
| Phone | `NVARCHAR(50)` |
| Salary | `DECIMAL(18,2)` |

The table also contains database constraints for non-empty names and phone numbers and for non-negative salaries.

## Connection String

The default connection string is stored in:

```text
src/ContactManager.Web/appsettings.json
```

Default configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=ContactManagerDb;Integrated Security=True;TrustServerCertificate=True;"
  }
}
```

If you use another SQL Server instance, update `DefaultConnection`.

## Build and Run

From the repository root:

```powershell
dotnet restore
dotnet build .\ContactManager.sln
dotnet run --project .\src\ContactManager.Web
```

The default HTTP development URL is:

```text
http://localhost:5186
```

The root route opens the Contact Manager page.

## CSV Format

Example:

```csv
Name,Date of birth,Married,Phone,Salary
Emily Johnson,1992-06-15,true,+15551234567,4200.50
Michael Davis,1988-11-03,false,+15559876543,3850.00
```

### Validation rules

**Name**
- Required.
- Maximum 200 characters.

**Date of birth**
- Required.
- Supported formats:
  - `yyyy-MM-dd`
  - `dd.MM.yyyy`
- Cannot be in the future.

**Married**
- Accepted values:
  - `true` / `false`
  - `yes` / `no`
  - `1` / `0`

**Phone**
- Required.
- Maximum 50 characters.
- Stored as a string so formatting symbols and leading `+` are preserved.

**Salary**
- Required.
- Must be a valid decimal number.
- Cannot be negative.
- Maximum two decimal places.
- Must fit SQL Server `DECIMAL(18,2)`.

Sample files are available in the `Samples` folder:

```text
Samples/contacts-valid.csv
Samples/contacts-invalid.csv
```

## Data Access

The project uses Dapper instead of Entity Framework.

Database access is separated behind:

```text
ISqlConnectionFactory
IContactRepository
ContactRepository
```

The repository contains parameterized SQL for:

- reading paged contacts;
- inserting imported contacts;
- updating contacts;
- deleting contacts.

User-provided values are passed as SQL parameters.

For server-side ordering, the requested sort column is mapped through an enum/whitelist instead of inserting arbitrary client input directly into `ORDER BY`.

## CSV Import Flow

```text
Browser upload
      ↓
ContactsController
      ↓
CsvImportService
      ↓
Validate headers and every row
      ↓
IContactRepository
      ↓
Dapper transaction
      ↓
SQL Server
```

If validation fails, the import is rejected and no contacts from that file are inserted.

## Server-side vs Client-side Sorting

The application intentionally demonstrates both approaches required by the task.

### Server-side
Clicking a table column header sends a new request to the server.

```text
Browser
  → ContactsController
  → ContactRepository
  → SQL ORDER BY
  → paged result
```

This ordering applies to the full database result set.

### Client-side
The Client-side tools panel uses JavaScript to filter and sort the rows already rendered on the current page.

No server request is required for these operations.

This keeps the distinction between scalable server-side pagination/ordering and instant client-side interactions clear.

## Inline Editing

Each row can be switched into edit mode without navigating to another page.

The browser submits the updated contact as JSON to the update endpoint. The backend validates the request before passing the contact to the Dapper repository.

Update and delete requests use ASP.NET Core antiforgery protection.

## Error Handling

The application handles common failure cases including:

- missing files;
- non-CSV uploads;
- files larger than 5 MB;
- empty CSV files;
- missing or duplicate CSV headers;
- malformed CSV structure;
- invalid individual rows;
- future dates of birth;
- invalid boolean values;
- invalid salary values;
- update validation errors;
- missing records during update/delete;
- unexpected import/database failures.

Unexpected server errors are logged with `ILogger`.

## Assignment Coverage

| Requirement | Implementation |
|---|---|
| ASP.NET MVC application | Yes |
| CSV upload | Yes |
| Backend CSV processing | Yes |
| MS SQL storage | Yes |
| Client-side filtering by columns | Yes |
| Client-side sorting by columns | Yes |
| Inline editing | Yes |
| Delete records | Yes |
| Basic validation | Yes |
| Error handling | Yes |
| Server-side pagination | Yes |
| Server-side ordering | Yes |
| Contact list ViewComponent | Yes |
| Header partial view | Yes |
| Footer partial view | Yes |
| Dapper instead of Entity Framework | Yes |
| SQL creation scripts in a separate folder | Yes |

## Notes

The sample implementation does not enforce contact uniqueness because the task does not define a unique key such as phone number or name/date combination. Importing the same valid CSV more than once therefore creates additional rows.

For a larger production system, possible next steps would include automated tests, authentication/authorization, database migrations, structured observability, configurable upload limits, and a dedicated API layer.
