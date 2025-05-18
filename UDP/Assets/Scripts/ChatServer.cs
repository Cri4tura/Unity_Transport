using UnityEngine;
using Unity.Networking.Transport;
using Unity.Collections;
using System.Collections.Generic;


/// <summary>
/// Servidor de chat UDP usando Unity Transport
/// </summary>
public class ChatServer : MonoBehaviour
{
    private NetworkDriver red;
    private NativeList<NetworkConnection> clientes;
    private Dictionary<NetworkConnection, FixedString64Bytes> nombresUsuarios;

    enum TipoMensaje : byte { Registro = 0, Chat = 1 }

    void Start()
    {
        red = NetworkDriver.Create();
        var puntoEscucha = NetworkEndpoint.AnyIpv4.WithPort(9000);
        if (red.Bind(puntoEscucha) != 0)
        {
            Debug.LogError("[Host] ❌ No se pudo abrir el puerto 9000");
            return;
        }

        red.Listen();
        clientes = new NativeList<NetworkConnection>(16, Allocator.Persistent);
        nombresUsuarios = new Dictionary<NetworkConnection, FixedString64Bytes>();

        Debug.Log("[Host] ✅ Servidor activo en puerto 9000");
    }

    void Update()
    {
        red.ScheduleUpdate().Complete();

        // Verifica desconexiones
        for (int i = clientes.Length - 1; i >= 0; i--)
        {
            if (!clientes[i].IsCreated)
            {
                Debug.Log($"[Host] 🔌 Usuario desconectado: {nombresUsuarios[clientes[i]]}");
                nombresUsuarios.Remove(clientes[i]);
                clientes.RemoveAtSwapBack(i);
                MostrarUsuariosConectados();
            }
        }

        // Acepta nuevas conexiones
        NetworkConnection nuevaConexion;
        while ((nuevaConexion = red.Accept()) != default)
        {
            clientes.Add(nuevaConexion);
            Debug.Log("[Host] 📶 Nueva conexión aceptada");
        }

        // Procesa eventos de red
        for (int i = 0; i < clientes.Length; i++)
        {
            NetworkEvent.Type tipoEvento;
            while ((tipoEvento = red.PopEventForConnection(clientes[i], out var lector)) != NetworkEvent.Type.Empty)
            {
                if (tipoEvento == NetworkEvent.Type.Data)
                {
                    var tipoMensaje = (TipoMensaje)lector.ReadByte();

                    if (tipoMensaje == TipoMensaje.Registro)
                    {
                        var nombre = lector.ReadFixedString64();
                        nombresUsuarios[clientes[i]] = nombre;
                        Debug.Log($"[Host] 🧍 Usuario registrado: {nombre}");
                        MostrarUsuariosConectados();
                    }
                    else if (tipoMensaje == TipoMensaje.Chat)
                    {
                        var contenido = lector.ReadFixedString512();
                        var remitente = nombresUsuarios[clientes[i]];
                        Debug.Log($"[Host] 💬 Mensaje de {remitente}: {contenido}");

                        // Reenvía el mensaje a todos los clientes conectados
                        for (int j = 0; j < clientes.Length; j++)
                        {
                            if (clientes[j].IsCreated)
                            {
                                int resultado = red.BeginSend(clientes[j], out var escritor);
                                if (resultado == 0)
                                {
                                    escritor.WriteByte((byte)TipoMensaje.Chat);
                                    escritor.WriteFixedString64(remitente);
                                    escritor.WriteFixedString512(contenido);
                                    red.EndSend(escritor);
                                }
                                else
                                {
                                    Debug.LogError($"[Host] ❌ Error al enviar a cliente {j}: {resultado}");
                                }
                            }
                        }
                    }
                }
                else if (tipoEvento == NetworkEvent.Type.Disconnect)
                {
                    clientes[i] = default; // Marca como desconectado
                }
            }
        }
    }

    /// <summary>
    /// Muestra en consola los usuarios conectados
    /// </summary>
    void MostrarUsuariosConectados()
    {
        string resumen = "[Host] 🧑‍🤝‍🧑 Usuarios conectados:\n";
        foreach (var par in nombresUsuarios)
        {
            resumen += $"- {par.Value}\n";
        }
        Debug.Log(resumen);
    }

    void OnDestroy()
    {
        red.Dispose();
        clientes.Dispose();
    }
}