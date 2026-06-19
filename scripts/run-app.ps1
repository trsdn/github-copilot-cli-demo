#requires -Version 7.0
<#
.SYNOPSIS
    Build and launch the InvoiceDropper WinUI app for the host architecture.

.DESCRIPTION
    Plain `dotnet run` on this WinUI 3 project hits an architecture / output-path
    quirk (it resolves the exe under bin\Debug instead of bin\<Platform>\Debug when
    Platform is set, and unpackaged bootstrap needs the matching RID). This helper
    builds explicitly for the host architecture and starts the built exe directly.

    Runs inline in the current terminal. Starting a GUI exe does not open a console
    window, so this is safe to run mid-demo.
#>
[CmdletBinding()]
param(
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

# Map the host processor architecture to the MSBuild Platform + .NET RID.
switch ($env:PROCESSOR_ARCHITECTURE) {
    'ARM64' { $platform = 'ARM64'; $rid = 'win-arm64' }
    'AMD64' { $platform = 'x64';   $rid = 'win-x64' }
    'x86'   { $platform = 'x86';   $rid = 'win-x86' }
    default { throw "Unsupported architecture: $($env:PROCESSOR_ARCHITECTURE)" }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$project  = Join-Path $repoRoot 'src/InvoiceDropper.WinUI/InvoiceDropper.WinUI.csproj'

Write-Host "Building InvoiceDropper.WinUI ($platform / $rid)..."
dotnet build $project -r $rid -p:Platform=$platform -c $Configuration --nologo
if ($LASTEXITCODE -ne 0) {
    # The WinUI XAML compiler (WMC9999) occasionally fails on incremental builds.
    # A single retry reliably clears it.
    Write-Host 'First build failed; retrying once...'
    dotnet build $project -r $rid -p:Platform=$platform -c $Configuration --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE."
    }
}

$binRoot = Join-Path $repoRoot "src/InvoiceDropper.WinUI/bin/$platform/$Configuration"
$exe = Get-ChildItem -Path $binRoot -Recurse -Filter 'InvoiceDropper.WinUI.exe' -ErrorAction SilentlyContinue |
    Where-Object { $_.FullName -match [regex]::Escape($rid) } |
    Select-Object -First 1

if (-not $exe) {
    throw "Could not find InvoiceDropper.WinUI.exe under $binRoot."
}

Write-Host "Launching $($exe.FullName)"
Start-Process -FilePath $exe.FullName
Write-Host 'App launched.'
