using UnityEngine;
using NativeWebSocket;
using System;
using System.Collections;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Unity.VisualScripting;

public class WebSocketClientServer : MonoBehaviour
{
    WebSocket websocket;

    string serverUri = "ws://localhost:6969";

    void Start()
    {
        Connect();
    }

    async void Connect()
    {
        websocket = new WebSocket(serverUri);

        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");    
            string playerId = IPAddress.Any.ToString();
            playerId = Hash128.Compute(playerId).ToString();
            Send($"join:{playerId}");
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

            // Deserialize received message
            MessageData data = JsonUtility.FromJson<MessageData>(message);

            // Handle different message types
            switch (data.command)
            {
                case "join":
                    Debug.Log(data.message);
                    break;
                case "move":
                    Debug.Log(data.message);
                    break;
                case "state":
                    Debug.Log(data.message);
                    break;
                default:
                    Debug.Log("Unknown command");
                    break;
            }
        };

        await websocket.Connect();
    }

    void OnDisable()
    {
        if (websocket != null)
        {
            string playerId = IPAddress.Any.ToString();
            playerId = Hash128.Compute(playerId).ToString();
            Send($"join:{playerId}");
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Send("join:Player1");
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            Send("move:Player1:left");
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            Send("move:Player1:right");
        }
    }

    private void OnDestroy()
    {
        if (websocket != null)
        {
            websocket.Close();
        }
    }

    // Data structure for deserializing messages
    [Serializable]
    public class MessageData
    {
        public string command;
        public string message;
    }
}
