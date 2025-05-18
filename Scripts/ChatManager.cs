using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using WebSocketSharp;
using Newtonsoft.Json;

public class ChatManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button sendButton;
    [SerializeField] private Transform messageContent; // Content del ScrollView
    [SerializeField] private GameObject messagePrefab; // Prefab del texto del mensaje
    [SerializeField] private ScrollRect scrollRect; // Referencia al ScrollRect

    private WebSocket ws;
    private List<string> messages = new List<string>();
    private bool newMessageReceived = false;
    private string lastSentMessage = null; // Almacena el último mensaje enviado

    void Start()
    {
        // Validar referencias
        if (inputField == null) Debug.LogError("InputField no asignado");
        if (sendButton == null) Debug.LogError("SendButton no asignado");
        if (messageContent == null) Debug.LogError("MessageContent no asignado");
        if (messagePrefab == null) Debug.LogError("MessagePrefab no asignado");
        else ValidatePrefab(messagePrefab);
        if (scrollRect == null) Debug.LogError("ScrollRect no asignado");

        // Conectar al servidor WebSocket
        ws = new WebSocket("ws://localhost:4000");
        ws.OnOpen += (sender, e) => Debug.Log("Conectado al WebSocket");
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log("Mensaje recibido: " + e.Data);
            lock (messages)
            {
                // Ignorar el mensaje si coincide con el último enviado
                if (e.Data != lastSentMessage)
                {
                    messages.Add(e.Data);
                    newMessageReceived = true;
                }
                else
                {
                    Debug.Log("Mensaje duplicado ignorado: " + e.Data);
                    lastSentMessage = null; // Limpiar después de ignorar
                }
            }
        };
        ws.OnError += (sender, e) => Debug.LogError("Error WebSocket: " + e.Message);
        ws.OnClose += (sender, e) => Debug.Log("WebSocket cerrado");
        ws.Connect();

        // Configurar botón de enviar
        sendButton.onClick.AddListener(SendMessage);
    }

    void Update()
    {
        // Actualizar UI solo desde el hilo principal
        if (newMessageReceived)
        {
            lock (messages)
            {
                UpdateMessagesUI();
                newMessageReceived = false;
                // Desplazar ScrollView al final
                if (scrollRect != null)
                {
                    Canvas.ForceUpdateCanvases();
                    scrollRect.verticalNormalizedPosition = 0f;
                    Debug.Log("ScrollView desplazado al final");
                }
            }
        }
    }

    void OnDestroy()
    {
        // Cerrar conexión WebSocket
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
        }
    }

    void UpdateMessagesUI()
    {
        // Limpiar mensajes actuales
        foreach (Transform child in messageContent)
        {
            Destroy(child.gameObject);
        }

        // Añadir mensajes nuevos
        foreach (string msg in messages)
        {
            if (messagePrefab == null)
            {
                Debug.LogError("MessagePrefab es null");
                return;
            }

            GameObject messageObj = Instantiate(messagePrefab, messageContent);
            TMP_Text textComponent = messageObj.GetComponent<TMP_Text>();
            if (textComponent != null)
            {
                textComponent.text = msg;
                Debug.Log("Mensaje añadido a UI: " + msg);
            }
            else
            {
                Debug.LogError("El prefab instanciado no tiene un componente TMP_Text. Verifica el prefab asignado.");
            }
        }
    }

    // Método para validar el prefab
    void ValidatePrefab(GameObject prefab)
    {
        if (prefab.GetComponent<TMP_Text>() == null)
        {
            Debug.LogError("El prefab asignado no tiene un componente TMP_Text. Usa un GameObject con TextMeshPro - Text (UI).");
        }
        else
        {
            Debug.Log("Prefab validado con TMP_Text.");
        }
    }

    public void SendMessage()
    {
        string message = inputField.text;
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("Mensaje vacío");
            return;
        }

        // Agregar el mensaje localmente y almacenarlo como último enviado
        lock (messages)
        {
            messages.Add(message);
            lastSentMessage = message; // Guardar el mensaje enviado
            newMessageReceived = true;
        }

        // Enviar mensaje mediante HTTP POST
        StartCoroutine(SendMessageCoroutine(message));
        inputField.text = "";
    }

    private IEnumerator SendMessageCoroutine(string message)
    {
        // Crear JSON para la solicitud
        var data = new { message = message };
        string json = JsonConvert.SerializeObject(data);

        // Configurar UnityWebRequest
        UnityWebRequest request = new UnityWebRequest("http://localhost:4000/api/message", "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        // Enviar solicitud
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Error al enviar mensaje: " + request.error);
        }
        else
        {
            Debug.Log("Mensaje enviado: " + message);
        }
    }
}