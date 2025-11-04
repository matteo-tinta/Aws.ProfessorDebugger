const http = require('http');
const fs = require('fs');
const path = require('path');

const port = 3000;
const publicDir = path.join(__dirname, 'public');

const server = http.createServer((req, res) => {
  let filePath;

  if (req.url === '/' || req.url === '') {
    filePath = path.join(publicDir, 'index.html');
  } else if (req.url.startsWith('/public/')) {
    filePath = path.join(__dirname, req.url);
  } else {
    // Redirect anything else to /public/
    filePath = path.join(publicDir, req.url);
  }

  const extname = path.extname(filePath).toLowerCase();
  const mimeTypes = {
    '.html': 'text/html',
    '.js': 'text/javascript',
    '.json': 'application/json',
    '.css': 'text/css',
    '.png': 'image/png',
    '.jpg': 'image/jpeg',
    '.gif': 'image/gif',
    '.svg': 'image/svg+xml'
  };

  const contentType = mimeTypes[extname] || 'application/octet-stream';

  fs.readFile(filePath, (error, content) => {
    if (error) {
      if (error.code === 'ENOENT') {
        res.writeHead(404, { 'Content-Type': 'text/plain' });
        res.end('404 Not Found');
      } else {
        res.writeHead(500, { 'Content-Type': 'text/plain' });
        res.end(`Server Error: ${error.code}`);
      }
    } else {
      res.writeHead(200, { 'Content-Type': contentType });
      res.end(content, 'utf-8');
    }
  });
});

server.listen(port, () => {
  console.log(`✅ Server running at http://localhost:${port}/`);
  console.log(`📂 Serving files from: ${publicDir}`);
});
