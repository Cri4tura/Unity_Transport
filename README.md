# Unity Transport & WebSocket Chat

Este proyecto demuestra la implementación de un sistema de chat en Unity utilizando diferentes tecnologías de red, incluyendo WebSockets y potencialmente Unity Transport. El proyecto incluye tanto la parte del cliente (Unity) como un servidor WebSocket simple implementado en Node.js.

## Descripción General

El proyecto se centra en dos funcionalidades principales:

1.  **Chat con WebSocket:**
    * Un cliente de Unity (`ChatManager.cs`) que se conecta a un servidor WebSocket.
    * Permite a los usuarios enviar y recibir mensajes en tiempo real.
    * La interfaz de usuario en Unity muestra los mensajes en un ScrollView.
    * El servidor WebSocket (`server/index.js`) está construido con Node.js, Express y la librería `ws`. Se encarga de recibir mensajes de los clientes y retransmitirlos a todos los demás clientes conectados.

2.  **Chat con Unity Transport (Opcional/Experimental):**
    * El script `NetworkManager.cs` sugiere una implementación de chat utilizando el paquete Unity Transport.
    * Este sistema parece manejar el registro de usuarios y el intercambio de mensajes de chat directamente entre clientes o a través de un servidor ligero implementado con Unity Transport.
    * Este componente podría ser una alternativa o un complemento al sistema de chat WebSocket.

El proyecto también incluye TextMeshPro para la renderización avanzada de texto en la interfaz de usuario de Unity.

## Características

### Cliente Unity (WebSocket - `ChatManager.cs`)
* Conexión a un servidor WebSocket (configurable a `ws://localhost:4000`).
* Envío de mensajes a través de un campo de entrada y un botón.
* Recepción y visualización de mensajes en tiempo real en un área de chat desplazable.
* Uso de prefabs para instanciar los mensajes en la UI.
* Manejo básico de errores y estados de conexión del WebSocket.
* Evita mostrar mensajes duplicados enviados por el mismo cliente.

### Cliente Unity (Unity Transport - `NetworkManager.cs`)
* Posibilidad de actuar como servidor o cliente basado en la entrada del usuario.
* Registro de nombre de usuario.
* Envío y recepción de mensajes de chat utilizando el sistema de transporte de Unity.
* Visualización de mensajes en un campo de texto.

### Servidor WebSocket (`server/index.js`)
* Servidor Node.js utilizando Express y `ws`.
* Escucha en el puerto `4000`.
* Maneja conexiones de nuevos clientes WebSocket.
* Recibe mensajes a través de una ruta POST (`/api/message`) y los retransmite a todos los clientes WebSocket conectados.
* Registra conexiones y desconexiones de clientes en la consola.

## Estructura del Proyecto

* **`Unity entrega ultima/Unity_Transport-main/`**: Raíz del proyecto.
    * **`README.md`**: Este archivo.
    * **`WebSocket/`**: Contiene el proyecto de Unity y el servidor Node.js.
        * **`Assets/`**: Directorio estándar de assets de Unity.
            * **`Scripts/`**:
                * `ChatManager.cs`: Gestiona la lógica del chat utilizando WebSockets y `websocket-sharp`.
                * `NetworkManager.cs`: Gestiona la lógica de red utilizando Unity Transport.
            * **`Prefabs/`**: Contiene prefabs como `MessageText.prefab` para la UI de los mensajes.
            * **`Scenes/`**: Contiene escenas como `ChatScene.unity`.
            * **`Plugins/`**: Incluye librerías como `websocket-sharp.dll`.
            * **`TextMesh Pro/`**: Assets y ejemplos de TextMeshPro.
        * **`ProjectSettings/`**: Archivos de configuración del proyecto Unity.
            * `ProjectVersion.txt`: Indica la versión de Unity utilizada (`2022.3.46f1`).
        * **`server/`**: Contiene el código del servidor WebSocket.
            * `index.js`: Punto de entrada principal del servidor.
            * `package.json`: Define las dependencias del servidor Node.js (express, ws, cors).
            * `package-lock.json`: Bloquea las versiones de las dependencias.

## Requisitos Previos

### Para el Cliente Unity:
* Unity Editor (versión `2022.3.46f1` o compatible).
* TextMeshPro (generalmente importado desde el Package Manager de Unity o incluido en `Assets`).
* Librería `websocket-sharp.dll` (incluida en `Assets/Plugins`).
* Paquete `Newtonsoft.Json` (para `ChatManager.cs`, puede necesitar ser añadido vía Package Manager si no está ya presente, o el código de serialización JSON podría ser diferente).
* Paquete `Unity.Networking.Transport` (para `NetworkManager.cs`, instalar desde el Package Manager de Unity).
* Paquete `Unity.Collections` (dependencia de Unity Transport).

### Para el Servidor WebSocket:
* Node.js y npm instalados.

## Configuración y Ejecución

### 1. Servidor WebSocket:
    1. Navega al directorio `Unity_Transport-main/WebSocket/server/`.
    2. Instala las dependencias:
       ```bash
       npm install
       ```
    3. Inicia el servidor:
       ```bash
       node index.js
       ```
    El servidor debería estar escuchando en `http://localhost:4000`.

### 2. Cliente Unity:
    1. Abre el proyecto `Unity_Transport-main/WebSocket/` con Unity Hub o Unity Editor.
    2. Abre la escena principal (probablemente `ChatScene` que se encuentra en `Assets/Scenes/`).
    3. **Para el chat WebSocket (`ChatManager`):**
        * Asegúrate de que el GameObject que contiene el script `ChatManager` tenga todas las referencias de UI (InputField, Button, MessageContent, MessagePrefab, ScrollRect) asignadas correctamente en el Inspector.
        * El script se conectará automáticamente al servidor WebSocket cuando se inicie la escena.
    4. **Para el chat con Unity Transport (`NetworkManager`):**
        * Asegúrate de que el GameObject que contiene el script `NetworkManager` tenga las referencias de UI (nameInputField, messageInputField, sendButton, connectButton, chatText) asignadas.
        * Para iniciar como servidor, escribe "server" en el campo de nombre antes de conectar. Para conectar como cliente, introduce cualquier otro nombre.
    5. Ejecuta la escena en el Editor de Unity.

## Uso

### Chat WebSocket:
1.  Una vez que el servidor Node.js esté en funcionamiento y la escena de Unity se esté ejecutando, el cliente Unity debería conectarse automáticamente al servidor WebSocket.
2.  Escribe mensajes en el campo de entrada y presiona el botón de enviar (o Enter) para enviar mensajes.
3.  Los mensajes enviados y recibidos de otros clientes conectados aparecerán en el área de chat.

### Chat Unity Transport:
1.  En la interfaz de usuario del `NetworkManager`, introduce un nombre. Si quieres actuar como servidor, introduce "server".
2.  Pulsa el botón "Conectar".
3.  Una vez conectado (ya sea como servidor o cliente), puedes usar el campo de entrada de mensajes y el botón de enviar para chatear.

## Notas Adicionales

* El proyecto utiliza **TextMeshPro** para la renderización de texto, lo que permite un formato de texto enriquecido y una mejor calidad visual. Se incluyen numerosos ejemplos y recursos de TextMeshPro.
* Se utiliza `websocket-sharp.dll` en el cliente de Unity para la comunicación WebSocket, mientras que el servidor usa el paquete `ws` de Node.js.
* El script `ChatManager.cs` también incluye lógica para enviar mensajes a través de HTTP POST al mismo servidor Node.js, que luego los difunde vía WebSocket. Esto podría ser una forma de integrar el envío de mensajes desde diferentes fuentes o para manejar la persistencia de mensajes si se implementara una base de datos en el servidor.
* El proyecto incluye varios scripts de ejemplo de TextMeshPro que no están directamente relacionados con la funcionalidad de chat pero forman parte del paquete importado.
* Hay referencias a EmojiOne para los sprites de emojis (), lo que indica que el proyecto podría soportar o tener la intención de soportar emojis.

## Posibles Mejoras
* Implementar autenticación de usuarios.
* Añadir persistencia de mensajes en el servidor (por ejemplo, con una base de datos).
* Mejorar la interfaz de usuario (UI/UX).
* Separar más claramente las funcionalidades de WebSocket y Unity Transport si están destinadas a ser dos sistemas de chat independientes.
* Manejo de errores más robusto en el lado del cliente y del servidor.
* Soporte para salas de chat o mensajes privados.

![image](https://github.com/user-attachments/assets/1ac717f9-e990-4a69-83dc-05d64fac2bbd)

