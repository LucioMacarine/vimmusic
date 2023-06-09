#pragma warning disable CS8618

using Serilog;
using vim.music.jsonstructs;
using Discord.Interactions;
using Newtonsoft.Json;

namespace vim.music
{
    public class GameMgMt
    {
        public static Game CurrentlyPlaying;

        public static OnProgressMatches MatchesClass;

        private static readonly string RunningPath = AppDomain.CurrentDomain.BaseDirectory;

        public static void SaveGame(Game game)
        {
            if (Directory.Exists(Path.Combine(RunningPath, "saves")))
            {
                Log.Warning("Diretório de saves não existente, criando...");
                Directory.CreateDirectory(Path.Combine(RunningPath, "saves"));
            }
            var json = JsonConvert.SerializeObject(game);
            File.WriteAllText(Path.Combine(RunningPath, "saves", $"{DateTime.Now.ToShortDateString()}-{DateTime.Now.ToShortTimeString()}-GAME.json"), json);
        }

        public static Game ProgressGame(Game game)
        {
            var newgame = new Game()
            {
                metrics = game.metrics
            };

            var winners = new List<DataRegistry>();
            foreach (var match in game.matches)
            {
                if (match.winner == 1) winners.Add(match.competidor1);
                else if (match.winner == 2) winners.Add(match.competidor2);
            }

            Log.Debug("winners: {winners}", winners);
            newgame.matches = MakeMatches(winners.ToArray());

            return newgame;
        }

        public static List<ParOrdenado> MakeMatches(DataRegistry[] competitors)
        {
            Random rnd = new Random();

            //embaralha a array
            competitors = competitors.OrderBy(c => rnd.Next()).ToArray();

            var competitorsCount = competitors.ToArray().Length;

            List<ParOrdenado> pares = new List<ParOrdenado>();

            for (int i = 0; i < competitorsCount; i += 2)
            {
                var ParOrdenadoOrdenado = competitors.Skip(i).Take(2).ToArray();
                Log.Debug("{penis}", ParOrdenadoOrdenado);
                var par = new ParOrdenado()
                {
                    competidor1 = ParOrdenadoOrdenado[0],
                    competidor2 = ParOrdenadoOrdenado[1]
                };
                pares.Add(par);
            }

            return pares;
        }

        public static string TextReport(Game game)
        {
            string output = "";

            foreach (var match in game.matches)
            {
                string line = $"{match.competidor1.MusicName} <VS> {match.competidor2.MusicName}";
                switch (match.winner)
                {
                    case 1:
                        line += $" => {match.competidor1.MusicName}";
                        break;
                    case 2:
                        line += $" => {match.competidor2.MusicName}";
                        break;
                }
                output += line + "\n";
            }

            return output;
        }
    }

    public class OnProgressMatches
    {
        public int CurrentVote;

        public List<Discord.IUser> Voters;

        public List<SocketInteractionContext> VoteMessages = new List<SocketInteractionContext>();

        public int CurrentMatchIndex;
    }
}