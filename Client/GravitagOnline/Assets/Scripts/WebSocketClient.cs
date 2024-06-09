using UnityEngine;
using NativeWebSocket;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

public class WebSocketClientServer : MonoBehaviour
{
    WebSocket websocket;
    string serverUri = "ws://gravitag.deketele.dev:6969";
    string playerId;

    void Start()
    {
        playerId = GeneratePlayerId();
        Connect();
    }

    async void Connect()
    {
        websocket = new WebSocket(serverUri);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");

            string messageJson = JsonUtility.ToJson(new MessageData { command = "join", playerId = playerId });

            Send(messageJson);
        };

        websocket.OnError += (e) =>
        {
            Debug.Log($"Error: {e}");
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            Debug.Log($"Received message: {message}");
            MessageData data = JsonUtility.FromJson<MessageData>(message);
            HandleMessage(data);
        };

        await websocket.Connect();
    }

    string GeneratePlayerId()
    {
        string ipAddress = IPAddress.Any.ToString();
        return Hash128.Compute(ipAddress).ToString();
    }

    void HandleMessage(MessageData data)
    {
        switch (data.command)
        {
            case "join":
                Debug.Log("Player joined the game!");
                break;
            case "move":
                Debug.Log(data.message);
                break;
            case "state":
                Debug.Log(data.message);
                // Update game state and UI based on the message
                break;
            case "error":
                Debug.Log(data.message);
                break;
            case "leave":
                Debug.Log("Player left the game!");
                break;
            default:
                Debug.Log("Unknown command: " + data.command);
                break;
        }
    }

    void OnDisable()
    {
        if (websocket != null)
        {
            string messageJson = JsonUtility.ToJson(new MessageData { command = "leave", playerId = playerId });
            Send(messageJson);
            websocket.Close();
        }
    }

    async void Send(string message)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(message);
        }
        else
        {
            Debug.Log("WebSocket is not open!");
        }
    }

    void Update()
    {
        websocket.DispatchMessageQueue();  // Make sure to call this regularly in the Unity main thread

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Send($"join:{playerId}");
        }

        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        input.Normalize();

        if (input != Vector2.zero)
        {
            Send($"move:{playerId}:{input.x}:{input.y}");
        }
    }

    [Serializable]
    public class MessageData
    {
        public string command;
        public string playerId;
        public string message;
    }
}
