param(
    [string]$Root = "/"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

Set-Location $PSScriptRoot

$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot ".dotnet-home"
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot ".nuget"
$env:APPDATA = Join-Path $PSScriptRoot ".appdata"

$nugetConfig = Join-Path $PSScriptRoot "NuGet.Config"
$project = (Resolve-Path (Join-Path $PSScriptRoot "src\Testify\Testify.fsproj")).Path
$input = (Resolve-Path (Join-Path $PSScriptRoot "site-docs")).Path
$output = Join-Path $PSScriptRoot "output\docs"

dotnet tool restore --configfile $nugetConfig
dotnet build $project --configfile $nugetConfig
pwsh -NoProfile -File (Join-Path $PSScriptRoot "verify-api-docs.ps1")

dotnet fsdocs build `
    --input $input `
    --projects $project `
    --output $output `
    --parameters root $Root fsdocs-collection-name Testify fsdocs-logo-src assets/logo-dino.png `
    --properties RestoreConfigFile=$nugetConfig RestorePackagesPath=$env:NUGET_PACKAGES `
    --clean
