import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import sharp from 'sharp'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const appDir = path.join(__dirname, '..')
const publicDir = path.join(appDir, 'public')
const buildDir = path.join(appDir, 'build')
const sourceIco = path.join(publicDir, 'icon.ico')

if (!fs.existsSync(sourceIco)) {
  console.error('icon.ico não encontrado em public/ — coloque o ícone em app/public/icon.ico')
  process.exit(1)
}

const ico = fs.readFileSync(sourceIco)
if (ico.length < 1000 || ico.readUInt16LE(2) !== 1) {
  console.error('public/icon.ico inválido ou corrompido')
  process.exit(1)
}

fs.mkdirSync(buildDir, { recursive: true })
fs.copyFileSync(sourceIco, path.join(buildDir, 'icon.ico'))

try {
  await sharp(sourceIco, { density: 256 })
    .resize(256, 256, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
    .png({ compressionLevel: 9 })
    .toFile(path.join(publicDir, 'icon.png'))
} catch {
  console.warn('Aviso: não foi possível atualizar icon.png a partir do .ico (mantido o existente).')
}

console.log('Ícone do instalador:', sourceIco, `(${ico.length} bytes)`)
console.log('Copiado para build/icon.ico')
