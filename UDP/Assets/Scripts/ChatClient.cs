using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChatClient : MonoBehaviour
{
    private NetworkDriver driver;
    private NetworkConnection conexion;
    private bool conectado;

    [SerializeField] private TMP_InputField campoMensaje;
    [SerializeField] private Button botonEnviar;
    [SerializeField] private TextMeshProUGUI areaMensajes;
    [SerializeField] private ScrollRect scrollChat;

    [SerializeField] private string nombreUsuario = "Invitado";

    private const string IPServidor = "127.0.0.1";
    private const ushort PuertoServidor = 9000;
    private const string PREFIJO = "[🔧 Chat UDP]";
    private const int LIMITE_MENSAJE = 512;

    enum TipoMensaje : byte { Registro = 0, Chat = 1 }

    void Start()
    {
        driver = NetworkDriver.Create();
        conexion = default;
        conectado = false;

        if (campoMensaje == null || botonEnviar == null || areaMensajes == null || scrollChat == null)
        {
            Debug.LogError("Faltan referencias UI");
            return;
        }

        botonEnviar.onClick.AddListener(EnviarMensaje);
        campoMensaje.onSubmit.AddListener(_ => EnviarMensaje());

        areaMensajes.text = $"{PREFIJO} Conectando al servidor...\n";
        Conectar();
    }

    void Conectar()
    {
        var endpoint = NetworkEndpoint.Parse(IPServidor, PuertoServidor);
        conexion = driver.Connect(endpoint);
        Debug.Log($"{PREFIJO} Intentando conexión a {IPServidor}:{PuertoServidor}");
    }

    void Update()
    {
        driver.ScheduleUpdate().Complete();

        if (!conexion.IsCreated && conectado)
        {
            conectado = false;
            areaMensajes.text += $"{PREFIJO} Desconectado del servidor.\n";
            DesplazarAbajo();
            return;
        }

        NetworkEvent.Type evento;
        while ((evento = driver.PopEventForConnection(conexion, out var stream)) != NetworkEvent.Type.Empty)
        {
            if (evento == NetworkEvent.Type.Connect)
            {
                conectado = true;
                areaMensajes.text += $"{PREFIJO} ¡Conectado al servidor!\n";
                EnviarRegistro();
                DesplazarAbajo();
            }
            else if (evento == NetworkEvent.Type.Data)
            {
                RecibirMensaje(stream);
            }
            else if (evento == NetworkEvent.Type.Disconnect)
            {
                conectado = false;
                conexion = default;
                areaMensajes.text += $"{PREFIJO} Conexión perdida.\n";
                DesplazarAbajo();
            }
        }
    }

    void EnviarRegistro()
    {
        if (!conexion.IsCreated) return;

        var result = driver.BeginSend(conexion, out var writer);
        if (result == 0)
        {
            writer.WriteByte((byte)TipoMensaje.Registro);
            writer.WriteFixedString64(nombreUsuario);
            driver.EndSend(writer);
        }
    }

    void EnviarMensaje()
    {
        if (!conectado || string.IsNullOrWhiteSpace(campoMensaje.text)) return;

        string texto = campoMensaje.text.Trim();

        if (texto.Length > LIMITE_MENSAJE)
        {
            areaMensajes.text += $"{PREFIJO} El mensaje supera los {LIMITE_MENSAJE} caracteres.\n";
            DesplazarAbajo();
            return;
        }

        if (texto.StartsWith("/"))
        {
            ProcesarComando(texto);
        }
        else
        {
            var result = driver.BeginSend(conexion, out var writer);
            if (result == 0)
            {
                writer.WriteByte((byte)TipoMensaje.Chat);
                writer.WriteFixedString512(texto);
                driver.EndSend(writer);
            }
        }

        campoMensaje.text = "";
        campoMensaje.ActivateInputField();
    }

    void ProcesarComando(string entrada)
    {
        var partes = entrada.Split(' ');
        string comando = partes[0].ToLower();

        if (comando == "/ayuda")
        {
            areaMensajes.text += $"{PREFIJO} Comandos disponibles:\n" +
                                 "- /ayuda: Ver ayuda\n" +
                                 "- /nombre <nuevo>: Cambia tu nombre\n" +
                                 "- /hora: Muestra la hora actual\n";
        }
        else if (comando == "/nombre")
        {
            if (partes.Length > 1)
            {
                string nuevoNombre = string.Join(" ", partes, 1, partes.Length - 1).Trim();
                if (!string.IsNullOrEmpty(nuevoNombre) && nuevoNombre.Length <= 64)
                {
                    nombreUsuario = nuevoNombre;
                    EnviarRegistro();
                    areaMensajes.text += $"{PREFIJO} Nombre cambiado a {nombreUsuario}\n";
                }
                else
                {
                    areaMensajes.text += $"{PREFIJO} Nombre inválido.\n";
                }
            }
            else
            {
                areaMensajes.text += $"{PREFIJO} Uso: /nombre <nuevo_nombre>\n";
            }
        }
        else if (comando == "/hora")
        {
            string hora = System.DateTime.Now.ToString("HH:mm:ss");
            areaMensajes.text += $"{PREFIJO} Hora del cliente: {hora}\n";
        }
        else
        {
            areaMensajes.text += $"{PREFIJO} Comando desconocido. Usa /ayuda\n";
        }

        DesplazarAbajo();
    }

    void RecibirMensaje(DataStreamReader stream)
    {
        var tipo = (TipoMensaje)stream.ReadByte();
        if (tipo == TipoMensaje.Chat)
        {
            var emisor = stream.ReadFixedString64();
            var texto = stream.ReadFixedString512();
            areaMensajes.text += $"<color=#00FF00>[{emisor}]</color>: {texto}\n";
            DesplazarAbajo();
        }
    }

    void DesplazarAbajo()
    {
        Canvas.ForceUpdateCanvases();
        scrollChat.verticalNormalizedPosition = 0f;
    }

    void OnDestroy()
    {
        if (driver.IsCreated)
            driver.Dispose();
    }
}