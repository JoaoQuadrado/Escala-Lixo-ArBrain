# Gera instalador Windows com API embutida (self-contained).
# Uso: .\scripts\build-installer.ps1

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $Root 'EscalaLixo.Api\EscalaLixo.Api.csproj'
$ApiBundle = Join-Path $Root 'app\api-bundle'
$AppDir = Join-Path $Root 'app'

Write-Host '>> Publicando API (win-x64, self-contained)...' -ForegroundColor Cyan
if (Test-Path $ApiBundle) { Remove-Item $ApiBundle -Recurse -Force }

dotnet publish $ApiProject `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -o $ApiBundle `
  /p:PublishSingleFile=false

$devConfig = Join-Path $Root 'EscalaLixo.Api\appsettings.Development.json'
$prodConfig = Join-Path $ApiBundle 'appsettings.json'
$exampleConfig = Join-Path $Root 'installer\appsettings.example.json'
$exampleDest = Join-Path $ApiBundle 'appsettings.example.json'

Copy-Item $exampleConfig $exampleDest -Force

if (Test-Path $devConfig) {
  Write-Host '>> Usando appsettings.Development.json no pacote da API.' -ForegroundColor Yellow
  Copy-Item $devConfig $prodConfig -Force
} else {
  Write-Host '>> Sem appsettings.Development.json — copiando exemplo.' -ForegroundColor Yellow
  Copy-Item $exampleConfig $prodConfig -Force
}

Write-Host '>> Build frontend + instalador Electron...' -ForegroundColor Cyan
Push-Location $AppDir
try {
  npm run build
  if ($LASTEXITCODE -ne 0) { throw 'npm run build falhou' }
  npx electron-builder --win
  if ($LASTEXITCODE -ne 0) { throw 'electron-builder falhou' }
} finally {
  Pop-Location
}

Write-Host ''
Write-Host 'Instalador gerado em app\release\' -ForegroundColor Green
Write-Host 'Copie o .exe para a outra máquina e instale — a API sobe automaticamente ao abrir o app.' -ForegroundColor Green
