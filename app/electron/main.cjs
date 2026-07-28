const { app, BrowserWindow, Menu, shell, ipcMain, dialog } = require('electron')
const path = require('path')
const http = require('http')
const {
  bundledApiExists,
  startBundledApi,
  stopBundledApi,
  getConfigHintPath,
} = require('./api-manager.cjs')
const { createStaticServer } = require('./static-server.cjs')

const isDev = !app.isPackaged
const DEV_PORTS = [5173, 5174, 5175]

/** @type {import('http').Server | null} */
let staticServer = null

function resolveIconPath() {
  const iconName = process.platform === 'win32' ? 'icon.ico' : 'icon.png'
  const baseDir = isDev ? path.join(__dirname, '../public') : path.join(__dirname, '../dist')
  return path.join(baseDir, iconName)
}

function probeDevServer(port) {
  return new Promise((resolve) => {
    const req = http.get(`http://localhost:${port}`, (res) => {
      res.resume()
      resolve(res.statusCode >= 200 && res.statusCode < 400)
    })
    req.on('error', () => resolve(false))
    req.setTimeout(800, () => {
      req.destroy()
      resolve(false)
    })
  })
}

async function resolveDevServerUrl() {
  if (process.env.VITE_DEV_SERVER_URL) {
    return process.env.VITE_DEV_SERVER_URL
  }

  for (const port of DEV_PORTS) {
    if (await probeDevServer(port)) {
      return `http://localhost:${port}`
    }
  }

  return 'http://localhost:5173'
}

async function createWindow() {
  const win = new BrowserWindow({
    width: 1280,
    height: 720,
    minWidth: 1280,
    minHeight: 720,
    center: true,
    show: false,
    title: 'Escala Lixo',
    icon: resolveIconPath(),
    backgroundColor: '#111827',
    frame: false,
    webPreferences: {
      preload: path.join(__dirname, 'preload.cjs'),
      contextIsolation: true,
      nodeIntegration: false,
    },
  })

  win.once('ready-to-show', () => {
    win.show()
    win.focus()
  })

  if (isDev) {
    const devUrl = await resolveDevServerUrl()
    await win.loadURL(devUrl)
    win.webContents.on('did-fail-load', () => {
      console.error(`Falha ao carregar ${devUrl}. Execute "npm run dev:full" ou "Iniciar-EscalaLixo.bat".`)
    })
  } else {
    const distDir = path.join(__dirname, '../dist')
    const { server, url } = await createStaticServer(distDir)
    staticServer = server
    await win.loadURL(url)
    win.webContents.on('did-fail-load', (_event, errorCode, errorDescription, validatedURL) => {
      console.error(`Falha ao carregar ${validatedURL}: ${errorCode} ${errorDescription}`)
    })
  }

  win.webContents.setWindowOpenHandler(({ url }) => {
    shell.openExternal(url)
    return { action: 'deny' }
  })
}

async function bootstrap() {
  await createWindow()

  if (!isDev && bundledApiExists()) {
    startBundledApi().catch((err) => {
      const configPath = getConfigHintPath()
      dialog.showErrorBox(
        'Escala Lixo — API não iniciou',
        `${err instanceof Error ? err.message : err}\n\n` +
          `Verifique a connection string do Supabase em:\n${configPath}\n\n` +
          'Edite o ficheiro, guarde e abra o app novamente.',
      )
    })
  }
}

app.whenReady().then(() => {
  Menu.setApplicationMenu(null)

  if (process.platform === 'darwin') {
    app.dock?.setIcon(resolveIconPath())
  }

  ipcMain.on('window-minimize', (event) => {
    BrowserWindow.fromWebContents(event.sender)?.minimize()
  })

  ipcMain.on('window-close', (event) => {
    BrowserWindow.fromWebContents(event.sender)?.close()
  })

  void bootstrap()
})

app.on('before-quit', () => {
  stopBundledApi()
  if (staticServer) {
    staticServer.close()
    staticServer = null
  }
})

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') app.quit()
})

app.on('activate', () => {
  if (BrowserWindow.getAllWindows().length === 0) void bootstrap()
})
