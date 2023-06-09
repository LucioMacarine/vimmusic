using System.Collections.Generic;

namespace vim.music.jsonstructs
{
    public class Configuration
    {
        public string Token { get; set; } = "";
        public int MaxFormAwnsersByPerson = -1;
        public TimerConfiguration timer = new TimerConfiguration();
    }

    public class TimerConfiguration
    {
        public bool enabled = false;
        public ulong timeUntilVoteClosesMS = 300000;
    }

    public class DataRegistry
    {
        public ulong userId { get; set; }
        public string Jogo { get; set; }
        public string MusicName { get; set; }
        public string MusicNameSerialized { get; set; }
        public string MusicLink { get; set; }
    }

    public class DataSet
    {
        public List<DataRegistry> entries { get; set; } = new List<DataRegistry>();
    }

    public class Games
    {
        public Game[] games;
    }

    public class Game
    {
        public List<ParOrdenado> matches = new List<ParOrdenado>();
        public GameMetrics metrics;
    }

    public class GameMetrics
    {
        public int matchesTotal;
        public int totalCompetitorCount;
        public List<ulong> Participants = new List<ulong>();
    }

    public class ParOrdenado
    {
        public int winner = 0;
        public DataRegistry competidor1;
        public DataRegistry competidor2;
    }
}
