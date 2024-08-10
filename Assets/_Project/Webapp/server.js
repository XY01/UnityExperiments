const WebSocket = require('ws');
const express = require('express');
const http = require('http');
const path = require('path');

const app = express();
const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

app.use(express.static(path.join(__dirname, 'public')));

app.get('/api/ui-elements', (req, res) => {
    // This should ideally come from your Unity application or a database
    const uiElements = [
        { id: "button1", type: "button", label: "Click Me" },
        { id: "slider1", type: "slider", min: 0, max: 100, value: 50 },
        { id: "slider2", type: "slider", min: 0, max: 100, value: 50 },
        { id: "slider3", type: "slider", min: 0, max: 100, value: 50 },
        { id: "button2", type: "button", label: "Click Me 2" }
    ];
    res.json(uiElements);
});

wss.on('connection', (ws) => {
    console.log('New WebSocket connection established');

    ws.on('message', (message) => {
        console.log('Received:', message);
        const parsedMessage = JSON.parse(message);

        // Broadcast the message to all connected clients (including Unity)
        wss.clients.forEach((client) => {
            if (client !== ws && client.readyState === WebSocket.OPEN) {
                client.send(JSON.stringify(parsedMessage));
            }
        });
    });

    ws.on('close', () => {
        console.log('WebSocket connection closed');
    });
});

const PORT = process.env.PORT || 3000;
server.listen(PORT, () => {
    console.log(`Server is running on http://localhost:${PORT}`);
});