import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import sharp from 'sharp'
import pngToIco from 'png-to-ico'

const __dirname = path.dirname(fileURLToPath(import.meta.url))
const appDir = path.join(__dirname, '..')
const source = path.join(appDir, 'electron', 'icon.png')
const publicDir = path.join(appDir, 'public')
const buildDir = path.join(appDir, 'build')

const sizes = [256, 128, 64, 48, 32, 16]

if (!fs.existsSync(source)) {
  console.error('Fonte não encontrada:', source)
  process.exit(1)
}

fs.mkdirSync(buildDir, { recursive: true })

const pngBuffers = await Promise.all(
  sizes.map(async (size) => {
    return sharp(source)
      .resize(size, size, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
      .png()
      .toBuffer()
  }),
)

const ico = await pngToIco(pngBuffers)
const iconIcoPath = path.join(publicDir, 'icon.ico')
const buildIcoPath = path.join(buildDir, 'icon.ico')

fs.writeFileSync(iconIcoPath, ico)
fs.writeFileSync(buildIcoPath, ico)

await sharp(source)
  .resize(256, 256, { fit: 'contain', background: { r: 0, g: 0, b: 0, alpha: 0 } })
  .png({ compressionLevel: 9 })
  .toFile(path.join(publicDir, 'icon.png'))

console.log('icon.ico', ico.length, 'bytes')
console.log('icon.png atualizado em public/')
