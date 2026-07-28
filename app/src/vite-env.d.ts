/// <reference types="vite/client" />

interface ElectronAPI {
  platform: string
  isElectron: boolean
  minimizeWindow?: () => void
  closeWindow?: () => void
}

interface Window {
  electronAPI?: ElectronAPI
}
