# Publie CursorCage puis compile l'installateur Inno Setup (CursorCage-Setup.exe).
# Prérequis : .NET SDK, Inno Setup 6 (iscc.exe dans le PATH ou définir $InnoCompiler)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path (Join-Path $root "CursorCage.csproj"))) {
    throw "CursorCage.csproj introuvable depuis $PSScriptRoot"
}

Set-Location $root

$publishDir = Join-Path $root "artifacts\publish"
if (Test-Path $publishDir) { Remove-Item $publishDir -Recurse -Force }
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null

Write-Host "dotnet publish..." -ForegroundColor Cyan
dotnet publish "$root\CursorCage.csproj" `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$iscc = $env:INNO_SETUP_COMPILER
if (-not $iscc) {
    $defaultIscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    if (Test-Path $defaultIscc) { $iscc = $defaultIscc }
}
if (-not $iscc) {
    $iscc = Get-Command iscc -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
}

if (-not $iscc -or -not (Test-Path $iscc)) {
    Write-Host ""
    Write-Host "Publication OK : $publishDir" -ForegroundColor Green
    Write-Host "Inno Setup 6 introuvable. Installez-le puis :" -ForegroundColor Yellow
    Write-Host '  & "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" Installer\CursorCage.iss' -ForegroundColor Gray
    exit 0
}

Write-Host "Inno Setup ($iscc)..." -ForegroundColor Cyan
& $iscc (Join-Path $root "Installer\CursorCage.iss")
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host ""
Write-Host "Installateur : $(Join-Path $root 'artifacts\installer\CursorCage-Setup.exe')" -ForegroundColor Green
