const { spawn } = require('child_process')
const fs = require('fs')
const http = require('http')
const path = require('path')

const API_HOST = '127.0.0.1'
const API_PORT = 5000
const HEALTH_URL = `http://${API_HOST}:${API_PORT}/api/health`

/** @type {import('child_process').ChildProcess | null} */
let apiProcess = null

function resolveBundledApiDir() {
  return path.join(process.resourcesPath, 'api')
}

function resolveBundledApiExe() {
  const dir = resolveBundledApiDir()
  const name = process.platform === 'win32' ? 'EscalaLixo.Api.exe' : 'EscalaLixo.Api'
  return path.join(dir, name)
}

function bundledApiExists() {
  return fs.existsSync(resolveBundledApiExe())
}

const INVALID_CONNECTION_MARKERS = [
  '[YOUR-PASSWORD]',
  'YOUR-PASSWORD',
  'SUA_SENHA',
  'SEU_HOST',
  'SEU_PROJETO',
  'senha_aqui',
  'COLOQUE_SUA_SENHA',
]

function readConnectionString(configPath) {
  try {
    const raw = fs.readFileSync(configPath, 'utf8')
    const parsed = JSON.parse(raw)
    return parsed?.ConnectionStrings?.DefaultConnection ?? ''
  } catch {
    return ''
  }
}

function isValidBundledConnection(connectionString) {
  if (!connectionString || typeof connectionString !== 'string') return false
  const normalized = connectionString.toLowerCase()
  return !INVALID_CONNECTION_MARKERS.some((marker) =>
    normalized.includes(marker.toLowerCase()),
  )
}

function resolveBundledConfigSource() {
  const bundled = path.join(resolveBundledApiDir(), 'appsettings.json')
  if (fs.existsSync(bundled) && isValidBundledConnection(readConnectionString(bundled))) {
    return bundled
  }

  const example = path.join(resolveBundledApiDir(), 'appsettings.example.json')
  if (fs.existsSync(example)) return example

  return bundled
}

function ensureAppDataConfig() {
  let configRoot
  if (process.platform === 'win32') {
    configRoot = process.env.APPDATA
  } else if (process.platform === 'darwin') {
    configRoot = path.join(process.env.HOME || '', 'Library', 'Application Support')
  } else {
    configRoot = process.env.XDG_CONFIG_HOME || path.join(process.env.HOME || '', '.config')
  }

  if (!configRoot) return null

  const appDir = path.join(configRoot, 'EscalaLixo')
  const configPath = path.join(appDir, 'appsettings.json')
  const bundledSource = resolveBundledConfigSource()

  fs.mkdirSync(appDir, { recursive: true })

  const shouldSeed =
    !fs.existsSync(configPath) ||
    !isValidBundledConnection(readConnectionString(configPath))

  if (shouldSeed && bundledSource && fs.existsSync(bundledSource)) {
    fs.copyFileSync(bundledSource, configPath)
  }

  return configPath
}

function probeHealth() {
  return new Promise((resolve) => {
    const req = http.get(HEALTH_URL, (res) => {
      res.resume()
      resolve(res.statusCode >= 200 && res.statusCode < 400)
    })
    req.on('error', () => resolve(false))
    req.setTimeout(1500, () => {
      req.destroy()
      resolve(false)
    })
  })
}

async function waitForApi(maxMs = 90000) {
  const start = Date.now()
  while (Date.now() - start < maxMs) {
    if (await probeHealth()) return
    await new Promise((r) => setTimeout(r, 400))
  }
  throw new Error('A API não respondeu a tempo.')
}

async function isApiRunning() {
  return probeHealth()
}

async function startBundledApi() {
  if (await isApiRunning()) return

  const exe = resolveBundledApiExe()
  if (!fs.existsSync(exe)) {
    throw new Error(`API não encontrada em ${exe}`)
  }

  const configPath = ensureAppDataConfig()
  const connectionString = configPath ? readConnectionString(configPath) : ''
  if (!isValidBundledConnection(connectionString)) {
    throw new Error(
      'Connection string inválida ou incompleta.\n' +
        `Edite ${configPath ?? '%APPDATA%\\EscalaLixo\\appsettings.json'} ` +
        'com os dados do Supabase.',
    )
  }

  let recentLogs = ''

  apiProcess = spawn(exe, [], {
    cwd: path.dirname(exe),
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Production',
      ASPNETCORE_URLS: `http://${API_HOST}:${API_PORT}`,
    },
    windowsHide: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  })

  const appendLog = (chunk) => {
    recentLogs = `${recentLogs}${chunk.toString()}`.slice(-4000)
  }

  apiProcess.stdout?.on('data', (chunk) => {
    appendLog(chunk)
    console.log(`[API] ${chunk.toString().trim()}`)
  })

  apiProcess.stderr?.on('data', (chunk) => {
    appendLog(chunk)
    console.error(`[API] ${chunk.toString().trim()}`)
  })

  const exitPromise = new Promise((resolve) => {
    apiProcess.on('exit', (code) => {
      if (code !== null && code !== 0) {
        console.error(`[API] encerrou com código ${code}`)
      }
      apiProcess = null
      resolve(code)
    })
  })

  try {
    await Promise.race([
      waitForApi(),
      exitPromise.then((code) => {
        if (code === null || code === 0) return
        const detail = recentLogs.trim() || `A API encerrou com código ${code}.`
        throw new Error(detail)
      }),
    ])
  } catch (err) {
    stopBundledApi()
    throw err
  }
}

function stopBundledApi() {
  if (!apiProcess || apiProcess.killed) return
  try {
    if (process.platform === 'win32') {
      spawn('taskkill', ['/pid', String(apiProcess.pid), '/f', '/t'], { windowsHide: true })
    } else {
      apiProcess.kill('SIGTERM')
    }
  } catch {
    /* ignore */
  }
  apiProcess = null
}

function getConfigHintPath() {
  if (process.platform === 'win32') {
    return path.join(process.env.APPDATA || '', 'EscalaLixo', 'appsettings.json')
  }
  if (process.platform === 'darwin') {
    return path.join(process.env.HOME || '', 'Library', 'Application Support', 'EscalaLixo', 'appsettings.json')
  }
  return path.join(process.env.XDG_CONFIG_HOME || path.join(process.env.HOME || '', '.config'), 'EscalaLixo', 'appsettings.json')
}

module.exports = {
  API_PORT,
  bundledApiExists,
  startBundledApi,
  stopBundledApi,
  isApiRunning,
  getConfigHintPath,
}
