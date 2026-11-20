param(
    [Parameter(Mandatory=$true)][string]$DalamudHome,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$resolvedSdk = (Resolve-Path -LiteralPath $DalamudHome).Path
$identity = [Reflection.AssemblyName]::GetAssemblyName((Join-Path $resolvedSdk 'Dalamud.dll'))
if ($identity.Version.Major -ne 13) { throw "Expected API13, got $($identity.Version)" }
$compilerVersion = & $Dotnet --version
if ([version]$compilerVersion -lt [version]'10.0.100') { throw 'C# 14 extension blocks require the .NET 10 compiler; output still targets .NET 9.' }
& $Dotnet build (Join-Path $PSScriptRoot '../ICE/ICE.csproj') -c Release "-p:DalamudLibPath=$resolvedSdk/"
if ($LASTEXITCODE -ne 0) { throw 'API13 candidate build failed' }
