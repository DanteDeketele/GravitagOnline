const WebSocket = require('ws');
const gameLogic = require('./gameLogic');

const wss = new WebSocket.Server({ port: 6969 });

wss.on('connection', (ws) => {
    console.log('Client connected');

    ws.on('message', (message) => {
        gameLogic.handleMessage(ws, message);
    });

    ws.on('close', () => {
        console.log('Client disconnected');
        gameLogic.handleDisconnect(ws);
    });
});
