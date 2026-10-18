# Publishes the prepared PADO ContactFlow sources to a pre-created GitHub feature branch.
# Run from a Windows PowerShell terminal after extracting PADO-ContactFlow.zip.
# Never uses force-push and never changes the main branch.
[CmdletBinding()]
param(
    [string]$Branch = 'feature/pado-contactflow-v1',
    [string]$Repository = 'https://github.com/PADOALEX/ContactManager.git',
    [switch]$SkipLocalChecks
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$destination = Join-Path (Split-Path -Parent $source) 'PADO-ContactFlow-GitHub-Publish'

function Invoke-Native([string]$Command, [string[]]$Arguments) {
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Command $($Arguments -join ' ') failed with exit code $LASTEXITCODE. Nothing was pushed."
    }
}

if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw 'Install Git for Windows first.' }
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue) -and -not $SkipLocalChecks) {
    throw 'Install the .NET 10 SDK first (or pass -SkipLocalChecks explicitly).'
}
if (-not (Test-Path (Join-Path $source 'PADO.ContactFlow.sln'))) {
    throw 'Run the script from the extracted PADO-ContactFlow archive, not from the old ContactManager project.'
}
if (Test-Path $destination) {
    throw "The temporary checkout already exists: $destination. Inspect it first; then delete or rename it to retry. The source folder has not been changed."
}

Write-Host "Cloning $Repository ($Branch) ..." -ForegroundColor Cyan
Invoke-Native git @('clone', '--single-branch', '--branch', $Branch, $Repository, $destination)
Push-Location $destination
try {
    # The remote main remains untouched; remove obsolete tracked files ONLY in this fresh checkout.
    Invoke-Native git @('rm', '-r', '--quiet', '--', '.')

    # Copy source, including hidden .github and .gitignore; exclude local build output and secrets.
    $excludedDirs = @('.git', '.vs', 'bin', 'obj', 'TestResults', 'node_modules')
    $excludedFiles = @('.env', 'secrets.json', 'appsettings.Local.json', 'appsettings.Production.json')
    foreach ($file in Get-ChildItem -LiteralPath $source -File -Force -Recurse) {
        $relative = $file.FullName.Substring($source.Length).TrimStart('\', '/')
        $parts = $relative -split '[\\/]'
        if (@($parts | Where-Object { $excludedDirs -contains $_ }).Count -gt 0) { continue }
        if ($excludedFiles -contains $file.Name -or $file.Name -match '^\.env\.(?!example$)') { continue }

        $target = Join-Path $destination $relative
        $targetDir = Split-Path -Parent $target
        if (-not (Test-Path -LiteralPath $targetDir)) {
            New-Item -ItemType Directory -Path $targetDir -Force | Out-Null
        }
        Copy-Item -LiteralPath $file.FullName -Destination $target -Force
    }

    if (-not (Test-Path (Join-Path $destination 'src/PADO.ContactFlow.Web/PADO.ContactFlow.Web.csproj'))) {
        throw 'The new application project is missing; nothing was pushed.'
    }
    if (-not (Test-Path (Join-Path $destination '.github/workflows/ci.yml'))) {
        throw 'GitHub Actions workflow is missing; nothing was pushed.'
    }

    if (-not $SkipLocalChecks) {
        Write-Host 'Checking .NET restore, Release build and non-SQL tests ...' -ForegroundColor Cyan
        Invoke-Native dotnet @('restore', 'PADO.ContactFlow.sln')
        Invoke-Native dotnet @('build', 'PADO.ContactFlow.sln', '-c', 'Release', '--no-restore')
        Invoke-Native dotnet @('test', 'PADO.ContactFlow.sln', '-c', 'Release', '--no-build', '--filter', 'Category!=SqlServer')
    }
    else {
        Write-Warning 'Local build and tests were explicitly skipped. Keep the Pull Request in Draft until CI passes.'
    }

    Invoke-Native git @('add', '--all')
    $staged = @(& git diff --cached --name-only)
    if ($LASTEXITCODE -ne 0) { throw 'Could not inspect staging; nothing was pushed.' }
    if (-not $staged -or $staged.Count -eq 0) { throw 'No changes detected. Nothing to push.' }
    foreach ($path in $staged) {
        if ($path -match '(^|/)(bin|obj|\.vs|TestResults|node_modules)/' -or
            $path -match '(^|/)\.env(\.|$)' -and $path -notmatch '(^|/)\.env\.example$' -or
            $path -match '(^|/)(appsettings\.Production\.json|appsettings\.Local\.json|secrets\.json)$') {
            throw "Unsafe file staged: $path. Nothing was pushed."
        }
    }

    Write-Host 'Staged changes:' -ForegroundColor Cyan
    Invoke-Native git @('diff', '--cached', '--stat')
    Invoke-Native git @('commit', '-m', 'feat: upgrade ContactManager to PADO ContactFlow')
    Invoke-Native git @('push', '-u', 'origin', $Branch)

    Write-Host 'Pushed source code to GitHub feature branch.' -ForegroundColor Green
    Write-Host 'Draft pull request: https://github.com/PADOALEX/ContactManager/pull/1'
    Write-Host 'Check both GitHub Actions jobs before merging to main.'
}
finally {
    Pop-Location
}
