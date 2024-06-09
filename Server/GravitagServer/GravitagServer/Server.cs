using System;
using System.Diagnostics;
using System.Net;
using System.Net.WebSockets;
using System.Text;

namespace GravitagServer
{
    internal class Server
    {
        public int PORT { get; }

        private List<Game> games = new List<Game>();
        private List<string> players = new List<string>();


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

            while (true)
            {
                

                HttpListenerContext context = await listener.GetContextAsync();
                if (context.Request.IsWebSocketRequest)
                {
                    await ProcessWebSocketRequest(context);
                }
                else
                {
                    Console.WriteLine( context.Request.HttpMethod + " request received. Sending 400 Bad Request...");
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }

                

            }

            foreach (Game game in games)
            {
                game.Stop();
            }
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = await context.AcceptWebSocketAsync(null);
            WebSocket socket = webSocketContext.WebSocket;

            while (socket.State == WebSocketState.Open)
            {
                var buffer = new byte[1024 * 4]; // Use a larger buffer size
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    Console.WriteLine($"Received message: {message}");

                    var response = HandleGameMessage(message);

                    // Send response back to the client
                    var responseBuffer = Encoding.UTF8.GetBytes(response);
                    await socket.SendAsync(new ArraySegment<byte>(responseBuffer, 0, responseBuffer.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                else if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                }
            }
        }


        private string HandleGameMessage(string message)
        {
            // Parse message and apply game logic
            var parts = message.Split(':');
            if (parts.Length < 2) return "Invalid message format";

            var command = parts[0];
            var player = parts[1];
            var argument = parts.Length > 2 ? parts[2] : null;

            switch (command)
            {
                case "join":
                    Game game = games.Find(g => !g.IsFull);

                    if (game == null)
                    {
                        CreateGame();
                        game = games[games.Count - 1];
                    }

                    game.AddPlayer(player);

                    return $"{player} joined the game {game.Id}";
                case "move":
                    if (argument != null)
                    {
                        game = games.Find(g => g.Players.Contains(player));
                        if (game == null) return "Player is not in a game";
                        game.PlayerMove(player, argument);
                        return $"{player} made a move: {argument}";
                    }
                    return "Move command requires an argument";
                case "state":
                    game = games.Find(g => g.Players.Contains(player));
                    if (game == null) return "Player is not in a game";
                    return game.GetGameState();
                case "leave":
                    game = games.Find(g => g.Players.Contains(player));
                    if (game == null) return "Player is not in a game";
                    game.PlayerDelete(player);
                    return $"{player} left the game {game.Id}";
                default:
                    return "Unknown command";
            }
        }

        private void PrintServerStartMessage()
        {
            string message = "--------------------------------\n";
            message += "Gravitag Server\n";
            message += "Created by: [Dante Deketele]\n";
            message += $".NET version: {Environment.Version}\n";
            message += $"Started at: {DateTime.Now}\n";
            message += $"OS Version: {Environment.OSVersion}\n";
            message += $"Cores #: {Environment.ProcessorCount} cores\n";
            message += $"Server is running in: {Environment.CurrentDirectory}\n";
            message += $"Username: {Environment.UserName}\n";
            message += $"UserDomainName: {Environment.UserDomainName}\n";
            message += $"Machine: {Environment.MachineName}\n";
            message += $"Port: {PORT}\n";
            message += "--------------------------------\n";

            Console.WriteLine(message);
        }

        private void CreateGame()
        {
            int id = games.Count;
            games.Add(new Game(id));
        }

        private void StopGame(int id)
        {
            Game game = games.Find(g => g.Id == id);

            if (game != null)
            {
                game.Stop();
                games.Remove(game);
            }
        }

        private void UpdateGames()
        {
            for (int i = games.Count - 1; i >= 0; i--)
            {
                Game game = games[i];
                game.Update();

                if (game.IsOver)
                {
                    game.Stop();
                    games.RemoveAt(i);
                }
            }
        }
    }
}
