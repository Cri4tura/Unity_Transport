const express = require("express");
const http = require("http");
const WebSocket = require("ws");
const cors = require("cors");

const app = express();
const port = 4000;

app.use(cors());
app.use(express.json());

const server = http.createServer(app);
const wss = new WebSocket.Server({ server });

wss.on("connection", (ws) => {
    console.log("Cliente conectado. Total clientes:", wss.clients.size);
    ws.on("close", () => {
        console.log("Cliente desconectado. Total clientes:", wss.clients.size);
    });
});

app.post("/api/message", (req, res) => {
    const { message } = req.body;
    console.log("Mensaje recibido:", message);
    if (!message) return res.status(400).json({ error: "Mensaje vacío" });

    wss.clients.forEach((client) => {
        if (client.readyState === WebSocket.OPEN) {
            client.send(message);
            console.log("Mensaje enviado a cliente:", message);
        }
    });

    res.json({ sent: true });
});

server.listen(port, () => {
    console.log(`Servidor escuchando en http://localhost:${port}`);
});
