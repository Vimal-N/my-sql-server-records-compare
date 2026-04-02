# Package Age Verification (PowerShell)
# ======================================
# Checks that all NuGet packages in Directory.Packages.props have been
# published for at least N days. Guards against supply chain attacks.
#
# Usage:
#   .\build\check-package-age.ps1              # default: 10 days
#   .\build\check-package-age.ps1 -MinDays 14  # custom: 14 days
#
# Bypass: Add entries to build\package-age-bypass.txt
#   Format: PackageId|Version|Reason

param(
    [int]$MinDays = 10
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$RootDir = Split-Path -Parent $ScriptDir
$PropsFile = Join-Path $RootDir "Directory.Packages.props"
$BypassFile = Join-Path $ScriptDir "package-age-bypass.txt"

Write-Host "Package Age Check - minimum $MinDays days" -ForegroundColor Cyan
Write-Host ("=" * 50)

if (-not (Test-Path $PropsFile)) {
    Write-Host "ERROR: Directory.Packages.props not found" -ForegroundColor Red
    exit 1
}

# Load bypass list
$Bypassed = @{}
if (Test-Path $BypassFile) {
    Get-Content $BypassFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#")) {
            $parts = $line.Split("|", 3)
            if ($parts.Length -ge 2) {
                $key = "$($parts[0].Trim())|$($parts[1].Trim())".ToLower()
                $reason = if ($parts.Length -ge 3) { $parts[2].Trim() } else { "no reason given" }
                $Bypassed[$key] = $reason
            }
        }
    }
}

# Parse packages from Directory.Packages.props
[xml]$props = Get-Content $PropsFile
$packages = $props.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -and $_.Version }

$failures = 0
$checked = 0

foreach ($pkg in $packages) {
    $pkgId = $pkg.Include
    $pkgVersion = $pkg.Version
    $checked++

    # Check bypass
    $bypassKey = "$pkgId|$pkgVersion".ToLower()
    if ($Bypassed.ContainsKey($bypassKey)) {
        Write-Host "  BYPASS  $pkgId $pkgVersion - $($Bypassed[$bypassKey])" -ForegroundColor Yellow
        continue
    }

    try {
        # Query NuGet API
        $url = "https://api.nuget.org/v3/registration5-gz-semver2/$($pkgId.ToLower())/$($pkgVersion.ToLower()).json"
        $response = Invoke-RestMethod -Uri $url -UseBasicParsing -ErrorAction Stop

        $published = $null
        if ($response.published) {
            $published = [DateTime]::Parse($response.published)
        } elseif ($response.catalogEntry -and $response.catalogEntry.published) {
            $published = [DateTime]::Parse($response.catalogEntry.published)
        }

        if (-not $published) {
            Write-Host "  WARN    $pkgId $pkgVersion - no publish date found" -ForegroundColor Yellow
            continue
        }

        $ageDays = ([DateTime]::UtcNow - $published).Days

        if ($ageDays -lt $MinDays) {
            Write-Host "  FAIL    $pkgId $pkgVersion - published $ageDays days ago (minimum: $MinDays)" -ForegroundColor Red
            $failures++
        } else {
            Write-Host "  OK      $pkgId $pkgVersion - published $ageDays days ago" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "  WARN    $pkgId $pkgVersion - could not fetch metadata: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

Write-Host ("=" * 50)

if ($failures -gt 0) {
    Write-Host "FAILED: $failures package(s) do not meet the $MinDays-day age requirement." -ForegroundColor Red
    Write-Host ""
    Write-Host "To bypass (e.g., urgent security fix), add to build\package-age-bypass.txt:" -ForegroundColor Yellow
    Write-Host "  PackageId|Version|Reason"
    exit 1
} else {
    Write-Host "PASSED: All $checked packages meet the $MinDays-day age requirement." -ForegroundColor Green
}
