using UnityEngine;
using UnityEngine.UI;
using Unity.Networking.Transport;
using System.Collections.Generic;
using Unity.Collections;
using TMPro;

public class NetworkManager : MonoBehaviour
{
    public enum MessageType : byte { Register = 0, Chat = 1 }

    // UI
    public TMP_InputField nameInputField;
    public TMP_InputField messageInputField;
    public Button sendButton;
    public Button connectButton;
    public TMP_Text chatText;

    // Red
    private NetworkDriver driver;
    private NativeList<NetworkConnection> connections;
    private Dictionary<NetworkConnection, FixedString64Bytes> playerNames;
    private bool isServer;
    private string serverAddress = "127.0.0.1";
    private ushort port = 9000;

    void Start()
    {
        connections = new NativeList<NetworkConnection>(Allocator.Persistent);
        playerNames = new Dictionary<NetworkConnection, FixedString64Bytes>();
        driver = NetworkDriver.Create();

        sendButton.onClick.AddListener(SendMessage);
        connectButton.onClick.AddListener(Connect);
        chatText.text = "";
    }

    void OnDestroy()
    {
        driver.Dispose();
        connections.Dispose();
    }

    void Update()
    {
        driver.ScheduleUpdate().Complete();

        if (isServer)
            UpdateServer();
        else
            UpdateClient();
    }

    void Connect()
    {
        isServer = nameInputField.text == "server"; // Escribe "server" para ser servidor
        if (isServer)
        {
            var endpoint = NetworkEndpoint.AnyIpv4.WithPort(port);
            if (driver.Bind(endpoint) != 0)
                Debug.LogError("Failed to bind to port");
            else
                driver.Listen();
            chatText.text += "Server started\n";
        }
        else
        {
            var endpoint = NetworkEndpoint.Parse(serverAddress, port);
            var connection = driver.Connect(endpoint);
            connections.Add(connection);
            SendRegisterMessage(nameInputField.text);
            chatText.text += "Connecting to server...\n";
        }
        connectButton.gameObject.SetActive(false);
    }

    void UpdateServer()
    {
        // Aceptar nuevas conexiones
        NetworkConnection connection;
        while ((connection = driver.Accept()) != default(NetworkConnection))
        {
            connections.Add(connection);
            Debug.Log("New client connected");
        }

        // Procesar mensajes
        for (int i = 0; i < connections.Length; i++)
        {
            if (!connections[i].IsCreated) continue;

            NetworkEvent.Type cmd;
            while ((cmd = driver.PopEventForConnection(connections[i], out var reader)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Data)
                {
                    var messageType = (MessageType)reader.ReadByte();
                    if (messageType == MessageType.Register)
                    {
                        var playerName = reader.ReadFixedString64();
                        playerNames[connections[i]] = playerName;
                        Debug.Log($"Player {playerName} registered");
                    }
                    else if (messageType == MessageType.Chat)
                    {
                        var message = reader.ReadFixedString512();
                        var senderName = playerNames[connections[i]];
                        var fullMessage = $"[{senderName}]: {message}";

                        // Reenviar a todos los clientes
                        for (int j = 0; j < connections.Length; j++)
                        {
                            if (connections[j].IsCreated)
                            {
                                DataStreamWriter writer;
                                if (driver.BeginSend(connections[j], out writer) == 0) // Fix: Use the correct overload
                                {
                                    writer.WriteByte((byte)MessageType.Chat);
                                    writer.WriteFixedString64(senderName);
                                    writer.WriteFixedString512(message);
                                    driver.EndSend(writer);
                                }
                            }
                        }
                        chatText.text += fullMessage + "\n";
                    }
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    connections[i] = default;
                    playerNames.Remove(connections[i]);
                }
            }
        }
    }

    void UpdateClient()
    {
        if (connections.Length == 0 || !connections[0].IsCreated) return;

        NetworkEvent.Type cmd;
        while ((cmd = driver.PopEventForConnection(connections[0], out var reader)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Data)
            {
                var messageType = (MessageType)reader.ReadByte();
                if (messageType == MessageType.Chat)
                {
                    var senderName = reader.ReadFixedString64();
                    var message = reader.ReadFixedString512();
                    chatText.text += $"[{senderName}]: {message}\n";
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                connections[0] = default;
                chatText.text += "Disconnected from server\n";
            }
        }
    }

    void SendMessage()
    {
        if (string.IsNullOrEmpty(messageInputField.text) || connections.Length == 0 || !connections[0].IsCreated) return;

        DataStreamWriter writer;
        if (driver.BeginSend(connections[0], out writer) == 0) // Use the correct overload with 'out' parameter  
        {
            writer.WriteByte((byte)MessageType.Chat);
            writer.WriteFixedString512(messageInputField.text);
            driver.EndSend(writer);
        }
        messageInputField.text = "";
    }

    void SendRegisterMessage(string playerName)
    {
        DataStreamWriter writer;
        if (driver.BeginSend(connections[0], out writer) == 0) // Use the correct overload with 'out' parameter  
        {
            writer.WriteByte((byte)MessageType.Register);
            writer.WriteFixedString64(playerName);
            driver.EndSend(writer);
        }
    }
}