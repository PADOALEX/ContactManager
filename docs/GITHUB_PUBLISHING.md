# Publish PADO ContactFlow to GitHub

The original repository is **PADOALEX/ContactManager**. The migration branch is **`feature/pado-contactflow-v1`**, created from `main` on 2026-10-18.

## One-command Windows publication

1. Extract `PADO-ContactFlow.zip` fully to a new folder.
2. Ensure Git for Windows and .NET 10 SDK are installed; configure `git config --global user.name` and `git config --global user.email` if necessary.
3. Open PowerShell **in the extracted PADO-ContactFlow folder**, then run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Publish-To-GitHub.ps1
```

The script clones the **existing feature branch** into a new sibling folder named `PADO-ContactFlow-GitHub-Publish`, replaces the obsolete tracked project **in that temporary checkout**, copies the 70+ project files (excluding local secrets/build outputs), performs `dotnet restore`, Release build and non-SQL tests, commits, and pushes. It **does not force-push or change `main`**.

If a check fails, the script aborts *before* pushing; post the console error for diagnosis. For a retry, inspect/rename/delete the temporary sibling folder created by the script, not the original extracted sources. `-SkipLocalChecks` is available only as an explicit fallback; do not merge until GitHub Actions becomes green.

## After pushing

1. Open the draft Pull Request at `https://github.com/PADOALEX/ContactManager/pull/1`.
2. Confirm the **build-and-unit-tests** and **sqlserver-integration** GitHub Actions jobs finish successfully.
3. Check the file diff: `ContactManager.Web` should be removed, `PADO.ContactFlow.Web` and test project should be present.
4. Keep the PR in draft if CI fails or the LocalDB CRUD/CSV manual smoke tests have not been performed.
5. Merge by squash only once checks and testing pass. Rename the repository if desired *after* migration; not required for the application to work.

No local Docker or PostgreSQL installation is needed to upload code. This project uses **SQL Server**, not PostgreSQL.
