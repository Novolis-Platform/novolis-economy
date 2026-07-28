#Requires -Version 7.0
param(
    [string]$Feed = $(if ($env:NOVOLIS_LOCAL_FEED) { $env:NOVOLIS_LOCAL_FEED } else { Join-Path (Split-Path $PSScriptRoot -Parent) "..\artifacts\nuget-local" })
)
$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$PackPackageCache = Join-Path $Root "..\artifacts\nuget-packages-pack"
New-Item -ItemType Directory -Force -Path $Feed | Out-Null
New-Item -ItemType Directory -Force -Path $PackPackageCache | Out-Null
$env:NUGET_PACKAGES = (Resolve-Path $PackPackageCache).Path
dotnet build-server shutdown 2>$null | Out-Null
dotnet pack (Join-Path $Root "Novolis.Economy.slnx") -c Release -o $Feed /p:ContinuousIntegrationBuild=false /p:NovolisLocalPack=true
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
Write-Host "Packed Novolis.Economy -> $Feed"
