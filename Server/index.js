const express = require('express');
const path = require('path');
const http = require('http');
const fs = require('fs');
const WebSocket = require('ws');
const gameLogic = require('./gameLogic');

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

const server = http.createServer(app);

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
