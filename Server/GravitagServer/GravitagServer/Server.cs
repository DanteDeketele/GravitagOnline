using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GravitagServer
{
    internal class Server
    {
        public int PORT { get; }
        private List<Game> _games = new List<Game>();
        private List<string> _players = new List<string>();
        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        public Server(int port)
        {
            PORT = port;
        }

        public async Task RunAsync()
        {
            Console.Title = "Gravitag Server";
            PrintServerStartMessage();
            CreateGame();

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add($"http://localhost:{PORT}/");
            listener.Start();

            Console.WriteLine("Server started. Waiting for connections...");

            // Start the game loop
            _ = Task.Run(() => GameLoop(_cancellationTokenSource.Token));

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    _ = ProcessWebSocketRequestAsync(context);
                }
                else
                {
                    Console.WriteLine($"{context.Request.HttpMethod} request received. Sending 400 Bad Request...");
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }

        private async Task ProcessWebSocketRequestAsync(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket socket = webSocketContext.WebSocket;

            try
            {
                while (socket.State == WebSocketState.Open)
                {
                    var buffer = new byte[1024 * 4];
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        var response = HandleGameMessage(message);
                        Debug.WriteLine($"Sending response: {response}");
                        response = "{\"command\":\"" + response + "\"}";
                        var responseBuffer = Encoding.UTF8.GetBytes(response);
                        await socket.SendAsync(new ArraySegment<byte>(responseBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebSocket error: {ex.Message}");
            }
            finally
            {
                if (socket.State != WebSocketState.Closed)
                {
                    socket.Abort();
                }
            }
        }

        private string HandleGameMessage(string message)
        {
            var parts = message.Split(':');
            if (parts.Length < 2) return "Invalid message format";

            var command = parts[0];
            var player = parts[1];
            var argument = parts.Length > 2 ? parts[2] : null;

            switch (command)
            {
                case "join":
                    var game = _games.Find(g => !g.IsFull);
                    if (game == null)
                    {
                        CreateGame();
                        game = _games[^1];
                    }
                    game.AddPlayer(player);
                    return $"join";

                case "move":
                    if (argument != null)
                    {
                        game = _games.Find(g => g.Players.Contains(player));
                        if (game == null) return "Player is not in a game";
                        game.PlayerMove(player, argument);
                        if (!_players.Contains(player))
                            _players.Add(player);
                        return $"{player} made a move: {argument}";
                    }
                    return "Move command requires an argument";

                case "state":
                    game = _games.Find(g => g.Players.Contains(player));
                    if (game == null) return "Player is not in a game";
                    return game.GetGameState();

                case "leave":
                    game = _games.Find(g => g.Players.Contains(player));
                    if (game == null) return "Player is not in a game";
                    game.PlayerDelete(player);
                    _players.Remove(player);
                    return $"leave";

                default:
                    return "Unknown command";
            }
        }

        private async Task GameLoop(CancellationToken token)
        {
            const int tickRate = 60; // 60 updates per second
            var updateInterval = TimeSpan.FromSeconds(1.0 / tickRate);
            var stopwatch = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {
                var startTime = stopwatch.Elapsed;

                // Update game state
                foreach (var game in _games)
                {
                    game.Update();
                    if (game.IsOver)
                    {
                        game.Stop();
                        _games.Remove(game);
                    }
                }

                var elapsed = stopwatch.Elapsed - startTime;
                var delay = updateInterval - elapsed;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, token);
                }
            }
        }

        private void PrintServerStartMessage()
        {
            var message = new StringBuilder();
            message.AppendLine("--------------------------------");
            message.AppendLine("Gravitag Server");
            message.AppendLine("Created by: [Dante Deketele]");
            message.AppendLine($".NET version: {Environment.Version}");
            message.AppendLine($"Started at: {DateTime.Now}");
            message.AppendLine($"OS Version: {Environment.OSVersion}");
            message.AppendLine($"Cores #: {Environment.ProcessorCount} cores");
            message.AppendLine($"Server is running in: {Environment.CurrentDirectory}");
            message.AppendLine($"Username: {Environment.UserName}");
            message.AppendLine($"UserDomainName: {Environment.UserDomainName}");
            message.AppendLine($"Machine: {Environment.MachineName}");
            message.AppendLine($"Port: {PORT}");
            message.AppendLine("--------------------------------");

            Console.WriteLine(message.ToString());
        }

        private void CreateGame()
        {
            int id = _games.Count;
            _games.Add(new Game(id));
        }

        private void StopGame(int id)
        {
            var game = _games.Find(g => g.Id == id);
            if (game != null)
            {
                game.Stop();
                _games.Remove(game);
            }
        }
    }
}
