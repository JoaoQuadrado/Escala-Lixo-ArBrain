# Gera instalador Windows com API embutida (self-contained).
# Uso: .\scripts\build-installer.ps1

$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $PSScriptRoot
$ApiProject = Join-Path $Root 'EscalaLixo.Api\EscalaLixo.Api.csproj'
$ApiBundle = Join-Path $Root 'app\api-bundle'
$AppDir = Join-Path $Root 'app'
$ReleaseDir = Join-Path $AppDir 'release'

function Stop-BuildLockProcesses {
  Get-Process -Name 'Escala Lixo', 'electron', 'EscalaLixo.Api' -ErrorAction SilentlyContinue |
    Stop-Process -Force -ErrorAction SilentlyContinue
}

function Clear-ReleaseDir {
  if (-not (Test-Path $ReleaseDir)) { return }

  Stop-BuildLockProcesses
  Start-Sleep -Seconds 1

  for ($i = 1; $i -le 5; $i++) {
    try {
      Remove-Item $ReleaseDir -Recurse -Force -ErrorAction Stop
      return
    } catch {
      Write-Host ">> Liberando app\release (tentativa $i/5)..." -ForegroundColor Yellow
      Stop-BuildLockProcesses
      Start-Sleep -Seconds 2
    }
  }

  throw @"
Nao foi possivel limpar app\release.
Feche o app Escala Lixo, janelas Electron e o Explorer nessa pasta, depois tente de novo.
"@
}

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
Clear-ReleaseDir

Write-Host '>> Gerando icon.ico...' -ForegroundColor DarkGray
Push-Location $AppDir
try {
  npm run icons:generate
  if ($LASTEXITCODE -ne 0) { throw 'icons:generate falhou' }
} finally {
  Pop-Location
}

# Evita EPERM em caminhos com acentos/espaço (ex.: "João Quadrado")
$BuildRoot = Join-Path $env:TEMP 'EscalaLixoBuild'
$ElectronOutput = Join-Path $BuildRoot 'release'
$env:ELECTRON_BUILDER_CACHE = Join-Path $BuildRoot 'cache'

Stop-BuildLockProcesses
if (Test-Path $BuildRoot) { Remove-Item $BuildRoot -Recurse -Force -ErrorAction SilentlyContinue }
New-Item -ItemType Directory -Force -Path $ElectronOutput, $env:ELECTRON_BUILDER_CACHE | Out-Null
Write-Host ">> Output temporário: $ElectronOutput" -ForegroundColor DarkGray

Push-Location $AppDir
try {
  npm run build
  if ($LASTEXITCODE -ne 0) { throw 'npm run build falhou' }

  npx electron-builder --win "--config.directories.output=$ElectronOutput"
  if ($LASTEXITCODE -ne 0) { throw 'electron-builder falhou' }

  $DestRelease = Join-Path $AppDir 'release'
  New-Item -ItemType Directory -Force -Path $DestRelease | Out-Null
  Get-ChildItem $ElectronOutput | Copy-Item -Destination $DestRelease -Recurse -Force
} finally {
  Pop-Location
}

Write-Host ''
Write-Host 'Instalador gerado em app\release\' -ForegroundColor Green
Write-Host 'Copie o .exe para a outra máquina e instale — a API sobe automaticamente ao abrir o app.' -ForegroundColor Green
