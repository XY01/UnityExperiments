const WebSocket = require('ws');
const express = require('express');
const http = require('http');
const path = require('path');

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

app.use(express.static(path.join(__dirname, 'public')));

let uiElements = [];

app.get('/api/ui-elements', (req, res) => {
    res.json(uiElements);
});

wss.on('connection', (ws) => {
    console.log('New WebSocket connection established');

    ws.on('message', (message) => {
        console.log('Received:', message);
        const parsedMessage = JSON.parse(message);

        if (parsedMessage.type === 'ui_elements') {
            uiElements = JSON.parse(parsedMessage.data).elements;
            broadcast({ type: 'ui_update', data: JSON.stringify(uiElements) });
        } else {
            // Broadcast the message to all connected clients (including Unity)
            broadcast(parsedMessage);
        }
    });

    ws.on('close', () => {
        console.log('WebSocket connection closed');
    });
});

function broadcast(message) {
    wss.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(JSON.stringify(message));
        }
    });
}

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});