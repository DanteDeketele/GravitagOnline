const express = require('express');
const path = require('path');
const fs = require('fs');
const https = require('https');
const WebSocket = require('ws');
const gameLogic = require('./gameLogic');

// Load SSL certificates
const privateKey = fs.readFileSync('/etc/letsencrypt/live/gravitag.deketele.dev/privkey.pem', 'utf8');
const certificate = fs.readFileSync('/etc/letsencrypt/live/gravitag.deketele.dev/cert.pem', 'utf8');
const ca = fs.readFileSync('/etc/letsencrypt/live/gravitag.deketele.dev/chain.pem', 'utf8');

const credentials = { key: privateKey, cert: certificate, ca: ca };

const app = express();

// Serve static files from the "public" directory
app.use(express.static(path.join(__dirname, 'public')));

app.get('/players', (req, res) => {
    // Return joined players and their IDs
    const players = gameLogic.getJoinedPlayers();
    res.setHeader('Content-Type', 'application/json');
    res.end(JSON.stringify(players));
});

// Serve the index.html page for all other routes
app.get('*', (req, res) => {
    res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

const httpsServer = https.createServer(credentials, app);

const wss = new WebSocket.Server({ server: httpsServer });

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

httpsServer.listen(6969, () => {
    console.log('Server running on port 6969 with HTTPS');
});
