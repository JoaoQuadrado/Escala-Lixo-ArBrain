const http = require('http')
const fs = require('fs')
const path = require('path')

const MIME_TYPES = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.mjs': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.gif': 'image/gif',
  '.svg': 'image/svg+xml',
  '.ico': 'image/x-icon',
  '.webp': 'image/webp',
  '.mp4': 'video/mp4',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2',
  '.map': 'application/json',
}

function contentType(filePath) {
  return MIME_TYPES[path.extname(filePath).toLowerCase()] || 'application/octet-stream'
}

function createStaticServer(rootDir, host = '127.0.0.1') {
  return new Promise((resolve, reject) => {
    const root = path.resolve(rootDir)

    const server = http.createServer((req, res) => {
      const url = new URL(req.url || '/', `http://${host}`)
      let rel = decodeURIComponent(url.pathname)
      if (rel === '/') rel = '/index.html'

      const safeSuffix = path.normalize(rel).replace(/^(\.\.[/\\])+/, '')
      const filePath = path.join(root, safeSuffix)

      if (!filePath.startsWith(root)) {
        res.writeHead(403)
        res.end('Forbidden')
        return
      }

      fs.stat(filePath, (err, stat) => {
        if (err || !stat.isFile()) {
          const indexPath = path.join(root, 'index.html')
          fs.stat(indexPath, (indexErr, indexStat) => {
            if (indexErr || !indexStat.isFile()) {
              res.writeHead(404)
              res.end('Not found')
              return
            }
            res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' })
            fs.createReadStream(indexPath).pipe(res)
          })
          return
        }

        res.writeHead(200, { 'Content-Type': contentType(filePath) })
        fs.createReadStream(filePath).pipe(res)
      })
    })

    server.on('error', reject)
    server.listen(0, host, () => {
      const { port } = server.address()
      resolve({
        server,
        url: `http://${host}:${port}`,
      })
    })
  })
}

module.exports = { createStaticServer }
