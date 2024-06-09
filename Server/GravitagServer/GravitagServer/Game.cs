using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GravitagServer
{
    internal class Game
    {
        public bool IsOver { get; set; }
        public int Id { get; }

        public List<string> Players { get; } = new List<string>();
        public int PlayerCount => Players.Count;
        public bool IsFull => PlayerCount >= 4;

        public Game(int id)
        {
            Start();
            IsOver = false;
            Id = id;
        }

        public void Start()
        {
            string Time = DateTime.Now.ToString("HH:mm:ss");
            string GameName = "Game " + Id;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Game {Id} started at {Time}");
            Console.ResetColor();
        }

        public void Update()
        {
            
        }
        
        public void Stop()
        {
            Console.WriteLine("Game stopped!");
        }

        public void AddPlayer(string player)
        {
            Players.Add(player);
            Console.WriteLine($"{player} joined game {Id}");
        }

        public void PlayerMove(string player, string move)
        {
            Console.WriteLine($"{player} made a move: {move}");
        }

        public void PlayerDelete(string player) {
            Players.Remove(player);
            Console.WriteLine($"{player} left game {Id}");
        }

        public string GetGameState()
        {
            return $"Game {Id} has {PlayerCount} players";
        }
    }
}
