$ErrorActionPreference = "Continue"

function Test-Command($Name) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { "OK $Name: $($cmd.Source)" } else { "WARN $Name not found" }
}

Write-Host "== Native Windows readiness =="
Test-Command dotnet
Test-Command node
Test-Command npm
Test-Command copilot

Write-Host "`n== .NET =="
dotnet --version
dotnet new list winui | Out-String

Write-Host "`n== Expected local paths =="
$paths = @(
    "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files (x86)\Windows Kits\10\Include\10.0.26100.0",
    "C:\Users\torstenmahr\.nuget\packages\microsoft.windowsappsdk\2.2.0"
)
foreach ($path in $paths) {
    if (Test-Path $path) { "OK $path" } else { "WARN Missing $path" }
}

Write-Host "`n== Copilot SDK bridge =="
npm run preflight --prefix tools/copilot-sdk-bridge

Write-Host "`n== Build smoke test =="
dotnet build InvoiceDropperDemo.sln$ErrorActionPreference = "Continue"

function Write-Check($Name, $Ok, $Detail) {
    $status = if ($Ok) { "OK" } else { "WARN" }
    Write-Host "$status $Name - $Detail"
}

$dotnet = Get-Command dotnet -ErrorAction SilentlyContinue
Write-Check ".NET SDK" ($null -ne $dotnet) ($(if ($dotnet) { dotnet --version } else { "dotnet not found" }))

$node = Get-Command node -ErrorAction SilentlyContinue
Write-Check "Node" ($null -ne $node) ($(if ($node) { node --version } else { "node not found" }))

$npm = Get-Command npm -ErrorAction SilentlyContinue
Write-Check "npm" ($null -ne $npm) ($(if ($npm) { npm --version } else { "npm not found" }))

$msbuildPath = "C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
Write-Check "MSBuild" (Test-Path $msbuildPath) $msbuildPath

$windowsSdk = "C:\Program Files (x86)\Windows Kits\10\Include\10.0.26100.0"
Write-Check "Windows SDK 10.0.26100.0" (Test-Path $windowsSdk) $windowsSdk

$winAppSdk = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.windowsappsdk\2.2.0"
Write-Check "Windows App SDK 2.2.0" (Test-Path $winAppSdk) $winAppSdk

$winuiTemplates = dotnet new list winui 2>$null
Write-Check "WinUI templates" ($LASTEXITCODE -eq 0 -and $winuiTemplates -match "WinUI Blank App") "dotnet new list winui"

$copilot = Get-Command copilot -ErrorAction SilentlyContinue
Write-Check "Copilot CLI" ($null -ne $copilot) ($(if ($copilot) { $copilot.Source } else { "copilot not found" }))

Push-Location tools/copilot-sdk-bridge
try {
    npm install | Out-Host
    npm run preflight | Out-Host
    Write-Check "Copilot SDK bridge preflight" ($LASTEXITCODE -eq 0) "npm run preflight"
}
finally {
    Pop-Location
}

dotnet build InvoiceDropperDemo.sln | Out-Host
Write-Check "Solution build" ($LASTEXITCODE -eq 0) "dotnet build InvoiceDropperDemo.sln"