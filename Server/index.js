const http = require('http');
const fs = require('fs');
const WebSocket = require('ws');
const gameLogic = require('./gameLogic');

const server = http.createServer((req, res) => {
    if (req.url === '/players' && req.method === 'GET') {
        // Return joined players and their IDs
        const players = gameLogic.getJoinedPlayers();
        res.setHeader('Content-Type', 'application/json');
        res.end(JSON.stringify(players));
    } else {
        // Serve the index.html page
        fs.readFile('./public/index.html', (err, data) => {
            if (err) {
                res.writeHead(404);
                res.end('File not found');
            } else {
                res.writeHead(200, { 'Content-Type': 'text/html' });
                res.end(data);
            }
        });
    }
});

const wss = new WebSocket.Server({ server });

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (message) => {
        console.log('Received:', message);
        gameLogic.handleMessage(ws, message);
    });

    ws.on('close', () => {
        console.log('Client disconnected');
        gameLogic.handleDisconnect(ws);
    });
});

server.listen(6969, () => {
    console.log('Server running on port 6969');
});
