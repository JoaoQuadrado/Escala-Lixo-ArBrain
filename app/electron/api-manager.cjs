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

  if (!fs.existsSync(configPath)) {
    fs.mkdirSync(appDir, { recursive: true })

    const candidates = [
      path.join(resolveBundledApiDir(), 'appsettings.example.json'),
      path.join(resolveBundledApiDir(), 'appsettings.json'),
    ]

    for (const src of candidates) {
      if (fs.existsSync(src)) {
        fs.copyFileSync(src, configPath)
        break
      }
    }
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

  ensureAppDataConfig()

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

  apiProcess.stdout?.on('data', (chunk) => {
    console.log(`[API] ${chunk.toString().trim()}`)
  })

  apiProcess.stderr?.on('data', (chunk) => {
    console.error(`[API] ${chunk.toString().trim()}`)
  })

  apiProcess.on('exit', (code) => {
    if (code !== null && code !== 0) {
      console.error(`[API] encerrou com código ${code}`)
    }
    apiProcess = null
  })

  await waitForApi()
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
