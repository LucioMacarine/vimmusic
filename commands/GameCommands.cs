using Serilog;
using Newtonsoft.Json;
using Discord.Interactions;
using Discord;
using System.Timers;

namespace vim.music.commands
{
    [Group("game", "Comandos relacionados ao GAME")]
    public class GameCommands : InteractionModuleBase<SocketInteractionContext>
    {
        private static System.Timers.Timer voteTimer = new System.Timers.Timer();
        public jsonstructs.Configuration Config { get; set; }
        public string RunningPath { get; set; }

        [DefaultMemberPermissions(Discord.GuildPermission.Administrator)]
        [SlashCommand("start", "inicia o jogo")]
        public async Task StartGame([Autocomplete(typeof(GameNumbersAutoCompleteHandler))] uint quantidade_de_musicas)
        {
            if (!System.Numerics.BitOperations.IsPow2(quantidade_de_musicas) || quantidade_de_musicas < 2)
            {
                var aembed = new EmbedBuilder()
                .WithImageUrl("https://media.discordapp.net/attachments/1113947801961898045/1114241759187304498/-ZxjyEq4yddVA6YhfB7GHtMviCbk4Ns3QiBKlr6FQwZXYFfF4d15fdFnXBQtcGoF1YjgimIvSZN56_Es600-nd-v1-rwa.png")
                .WithTitle("Erro")
                .WithColor(Color.Red)
                .WithDescription("Bota uma potência de 2 por favor")
                .Build();
                await Context.Interaction.RespondAsync(embed: aembed, ephemeral: true);
                return;
            }

            var json = File.ReadAllText(Path.Combine(RunningPath, "pre_musics.json"));
            var competidores = JsonConvert.DeserializeObject<jsonstructs.DataSet>(json).entries;

            if (quantidade_de_musicas > competidores.ToArray().Length)
            {
                var aembed = new EmbedBuilder()
                .WithTitle("Erro")
                .WithColor(Color.Red)
                .WithDescription("Não há músicas o suficiente para cumprir a quantidade desejada")
                .WithFooter(new EmbedFooterBuilder().WithText("e eu tô com preguiça d programar o treco pra fazer isso automaticamentekkkkkk"))
                .Build();
                await Context.Interaction.RespondAsync(embed: aembed, ephemeral: true);
                return;
            }

            Log.Debug("Iniciando NOVO jogo");

            Random rnd = new Random();
            competidores = competidores.OrderBy(c => rnd.Next()).ToList();
            competidores = competidores.Take((int)quantidade_de_musicas).ToList();

            GameMgMt.CurrentlyPlaying = new jsonstructs.Game()
            {
                matches = GameMgMt.MakeMatches(competidores.ToArray()),
                metrics = new jsonstructs.GameMetrics()
                {
                    matchesTotal = System.Numerics.BitOperations.Log2((uint)competidores.ToArray().Length),
                    totalCompetitorCount = competidores.ToArray().Length
                }
            };

            int roundCount = 0;
            for (int i = competidores.ToArray().Length / 2; i > 1; i = i / 2)
            {
                roundCount++;
            }

            var field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("**Músicas:**").WithValue("```" + competidores.ToArray().Length + "```");
            var field2 = new EmbedFieldBuilder().WithIsInline(true).WithName("**Rounds restantes:**").WithValue("```" + roundCount + "```");

            var embed = new EmbedBuilder().WithTitle("Iniciando jogo").WithFields(new EmbedFieldBuilder[] { field1, field2 }).WithColor(Color.Blue).Build();

            var matches = new EmbedBuilder().WithTitle("Partidas para a fase:").WithDescription($"```{GameMgMt.TextReport(GameMgMt.CurrentlyPlaying)}```").WithColor(Color.Blue).Build();

            GameMgMt.MatchesClass = new OnProgressMatches();

            await Context.Interaction.RespondAsync(embeds: new Embed[] { embed, matches }, ephemeral: true);
        }

        [HaveGameInitializedForFucksSake]
        [DefaultMemberPermissions(Discord.GuildPermission.Administrator)]
        [SlashCommand("vote", "Inicia uma votação")]
        public async Task StartVote()
        {
            var match = GameMgMt.CurrentlyPlaying.matches.Find(x => x.winner == 0);

            if (match == null)
            {
                var aembed = new EmbedBuilder().WithColor(Color.Red).WithTitle("Erro").WithDescription("Nenhuma partida precisa de votação.\nPor favor cheque se o bloco de partidas já foi finalizado");
                await Context.Interaction.RespondAsync(embed: aembed.Build(), ephemeral: true);
                return;
            }

            GameMgMt.MatchesClass.CurrentMatchIndex = GameMgMt.CurrentlyPlaying.matches.FindIndex(a => a == match);

            var field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("Competidor 1:").WithValue($"```({match.competidor1.Jogo})\n\n{match.competidor1.MusicName}```");
            var field2 = new EmbedFieldBuilder().WithIsInline(true).WithName("-").WithValue("**VERSUS**");
            var field3 = new EmbedFieldBuilder().WithIsInline(true).WithName("Competidor 2:").WithValue($"```({match.competidor2.Jogo})\n\n{match.competidor2.MusicName}```");

            var footer = new EmbedFooterBuilder().WithText("Aperte o botão correspondente para fazer o seu voto");

            var embed = new EmbedBuilder().WithTitle("Votar.").WithColor(Color.DarkBlue).WithFields(new EmbedFieldBuilder[] { field1, field2, field3 }).WithFooter(footer).Build();

            var components = new ComponentBuilder()
            .WithButton("Link da música 1", style: ButtonStyle.Link, url: match.competidor1.MusicLink)
            .WithButton("Link da música 2", style: ButtonStyle.Link, url: match.competidor2.MusicLink)
            .WithButton("1", "vote_competitor_1", emote: new Emoji("✅"), row: 1)
            .WithButton("2", "vote_competitor_2", emote: new Emoji("✅"), row: 1);

            GameMgMt.MatchesClass.CurrentVote = 0;
            GameMgMt.MatchesClass.Voters = new List<IUser>();
            await Context.Interaction.RespondAsync(embeds: new Embed[] { embed }, components: components.Build());

            GameMgMt.MatchesClass.VoteMessages.Add(Context);

            if (Config.timer.enabled)
            {
                voteTimer = new System.Timers.Timer(Config.timer.timeUntilVoteClosesMS);
                voteTimer.Elapsed += AutoEndVoting;
                voteTimer.AutoReset = false;
                voteTimer.Start();
            }
        }

        [HaveGameInitializedForFucksSake]
        [ComponentInteraction("vote_competitor_*", ignoreGroupNames: true)]
        public async Task HandleVote(string n)
        {
            if (GameMgMt.MatchesClass.Voters.Where(x => x.Id == Context.Interaction.User.Id).ToArray().Length > 0)
            {
                var aembed = new EmbedBuilder().WithColor(Color.Red).WithTitle("Erro").WithDescription("Você já votou bro.").Build();
                await Context.Interaction.RespondAsync(embed: aembed, ephemeral: true);
                return;
            }

            switch (n)
            {
                case "1":
                    GameMgMt.MatchesClass.CurrentVote++;
                    break;
                case "2":
                    GameMgMt.MatchesClass.CurrentVote--;
                    break;
            }
            GameMgMt.MatchesClass.Voters.Add(Context.Interaction.User);

            Log.Information("{user} votou {score}. Total: {score_total}", Context.Interaction.User.Username, n, GameMgMt.MatchesClass.CurrentVote);

            var embed = new EmbedBuilder().WithColor(Color.Green).WithTitle("Sucesso").WithDescription("Você votou na opção: " + n).Build();
            await Context.Interaction.RespondAsync(embed: embed, ephemeral: true);

            if (GameMgMt.CurrentlyPlaying.metrics.Participants.Where(x => x == Context.Interaction.User.Id).ToArray().Length <= 0)
            {
                GameMgMt.CurrentlyPlaying.metrics.Participants.Add(Context.Interaction.User.Id);
            }
        }

        [HaveGameInitializedForFucksSake]
        [DefaultMemberPermissions(Discord.GuildPermission.Administrator)]
        [SlashCommand("vote_end", "Encerra uma votação")]
        public async Task ManualEndVoting()
        {
            await Context.Interaction.DeferAsync();
            voteTimer.Enabled = false;
            voteTimer.Dispose();
            await EndVoting();

            foreach (var message in GameMgMt.MatchesClass.VoteMessages)
            {
                var aembed = new EmbedBuilder().WithTitle("Votação encerrada").WithColor(Color.DarkBlue);

                await message.Interaction.ModifyOriginalResponseAsync(a =>
                {
                    a.Embed = aembed.Build();
                    a.Components = new ComponentBuilder().Build();
                });
            }
        }

        public async Task EndVoting()
        {
            var match = GameMgMt.CurrentlyPlaying.matches[GameMgMt.MatchesClass.CurrentMatchIndex];

            var finalscore = GameMgMt.MatchesClass.CurrentVote;

            jsonstructs.DataRegistry winnerMusic;

            EmbedFieldBuilder field1 = new EmbedFieldBuilder();
            if (finalscore >= 0)
            {
                match.winner = 1;
                winnerMusic = match.competidor1;
            }
            else
            {
                match.winner = 2;
                winnerMusic = match.competidor2;
            }

            field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("**Vencedor:**").WithValue($"```({winnerMusic.Jogo})\n\n{winnerMusic.MusicName}```");

            if (GameMgMt.CurrentlyPlaying.matches.ToArray().Length <= 1)
            {
                var MusicSuggester = await Context.Client.GetUserAsync(winnerMusic.userId);

                var embed1field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("Vencedor:").WithValue($"||```{winnerMusic.MusicName}```||");

                var embed1 = new EmbedBuilder()
                .WithTitle("**Jogo encerrado** 🥳🥳🥳")
                .WithColor(Color.Gold)
                .WithFields(new EmbedFieldBuilder[] { embed1field1 })
                .Build();

                var embed2field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("Partidas totais:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.matchesTotal}```");
                var embed2field2 = new EmbedFieldBuilder().WithIsInline(true).WithName("Músicas totais:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.totalCompetitorCount}```");
                var embed2field3 = new EmbedFieldBuilder().WithIsInline(true).WithName("Total de participantes:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.Participants.ToArray().Length}```");

                var embed2 = new EmbedBuilder()
                .WithTitle("Estatísticas:")
                .WithColor(Color.Gold)
                .WithFields(new EmbedFieldBuilder[] { embed2field1, embed2field2, embed2field3 })
                .Build();

                var suggester = await Context.Client.GetUserAsync(winnerMusic.userId);

                var embed3field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("Música sugerida por:").WithValue(suggester.Mention);

                var embed3 = new EmbedBuilder()
                .WithTitle("Metadados da música")
                .WithColor(Color.Gold)
                .WithFields(new EmbedFieldBuilder[] { embed3field1 }).Build();

                await Context.Interaction.ModifyOriginalResponseAsync(x =>
                {
                    x.Embeds = new Embed[] { embed1, embed2, embed3 };
                    x.Components = new ComponentBuilder().Build();
                });
                return;
            }

            var embed = new EmbedBuilder().WithTitle("Votação encerrada").WithColor(Color.DarkTeal).WithFields(new EmbedFieldBuilder[] { field1 });
            await Context.Interaction.ModifyOriginalResponseAsync(x =>
            {
                x.Embed = embed.Build();
                x.Components = new ComponentBuilder().Build();
            });

            if (GameMgMt.CurrentlyPlaying.matches.Where(x => x.winner == 0).ToArray().Length <= 0)
            {
                var aembed = new EmbedBuilder().WithTitle("Bloco de partidas encerrado").WithColor(Color.Orange).WithDescription("Esse bloco de partidas foi concluído\nPor favor inicie o próximo bloco usando /game progress_game\nO jogo será salvo quando o próximo bloco for criado\nVocê pode ver os resultados desse bloco com /game status");
                await Context.Interaction.FollowupAsync(embed: aembed.Build(), ephemeral: true);
            }
        }

        public async void AutoEndVoting(object sender, ElapsedEventArgs e)
        {
            Log.Debug("yahar.png");
            await EndVoting();
        }

        [HaveGameInitializedForFucksSake]
        [SlashCommand("status", "Ver o status da partida atual")]
        public async Task BracketBox()
        {
            var embed = new EmbedBuilder().WithColor(Color.DarkBlue).WithTitle("Status da partida atual:").WithDescription("```" + GameMgMt.TextReport(GameMgMt.CurrentlyPlaying) + "```");
            await Context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        [HaveGameInitializedForFucksSake]
        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("progress_game", "Inicia um novo bloco de partidas com os ganhadores atuais")]
        public async Task ProgressGame()
        {
            if (GameMgMt.CurrentlyPlaying.matches.Where(x => x.winner == 0).ToArray().Length > 0)
            {
                var aembed = new EmbedBuilder().WithColor(Color.Red).WithTitle("Erro").WithDescription("Nem todas as partidas do bloco atual foram concluídas.\nPor favor vote para essas partidas e tente novamente quando todas terem um ganhador definido");
                await Context.Interaction.RespondAsync(embed: aembed.Build(), ephemeral: true);
                return;
            }

            int roundCount = 0;
            GameMgMt.CurrentlyPlaying = GameMgMt.ProgressGame(GameMgMt.CurrentlyPlaying);

            roundCount = 0;
            for (int i = GameMgMt.CurrentlyPlaying.matches.ToArray().Length / 2; i > 1; i = i / 2)
            {
                roundCount++;
            }

            var field2 = new EmbedFieldBuilder().WithIsInline(true).WithName("**Rounds restantes:**").WithValue("```" + roundCount + "```");

            var embed = new EmbedBuilder().WithTitle("Iniciando novo bloco de partidas").WithFields(new EmbedFieldBuilder[] { field2 }).WithColor(Color.Blue).WithDescription("Seu jogo será salvo automaticamente.\nPara confirmar o salvamento por favor cheque a pasta ./saves").Build();

            var matches = new EmbedBuilder().WithTitle("Partidas para a fase:").WithDescription($"```{GameMgMt.TextReport(GameMgMt.CurrentlyPlaying)}```").WithColor(Color.Blue).Build();

            GameMgMt.MatchesClass = new OnProgressMatches();

            await Context.Interaction.RespondAsync(embeds: new Embed[] { embed, matches }, ephemeral: true);

            GameMgMt.SaveGame(GameMgMt.CurrentlyPlaying);
        }

        [DefaultMemberPermissions(GuildPermission.Administrator)]
        [SlashCommand("load", "Carrega um jogo de um arquivo salvo")]
        public async Task LoadGame([Autocomplete(typeof(SaveGamesAutoCompleteHandler))] string file)
        {
            var json = await File.ReadAllTextAsync(Path.Combine(RunningPath, "saves", file));
            var game = JsonConvert.DeserializeObject<jsonstructs.Game>(json);

            GameMgMt.CurrentlyPlaying = game;
            GameMgMt.MatchesClass = new OnProgressMatches();

            var field1 = new EmbedFieldBuilder().WithIsInline(true).WithName("Total de partidas do jogo:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.matchesTotal}```");
            var field2 = new EmbedFieldBuilder().WithIsInline(true).WithName("Participantes até agora:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.Participants.ToArray().Length}```");
            var field3 = new EmbedFieldBuilder().WithIsInline(true).WithName("Total de músicas nesse jogo:").WithValue($"```{GameMgMt.CurrentlyPlaying.metrics.totalCompetitorCount}```");

            var embed = new EmbedBuilder().WithColor(Color.Blue).WithTitle("Jogo carregado com êxito").WithFields(new EmbedFieldBuilder[] { field1, field2, field3 }).WithDescription("Você pode chegar o jogo com /game status");

            await RespondAsync(embed: embed.Build(), ephemeral: true);
        }

        public class GameNumbersAutoCompleteHandler : AutocompleteHandler
        {
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                var results = new List<AutocompleteResult>();

                Log.Debug(autocompleteInteraction.Data.Current.Value.ToString());

                if (UInt32.TryParse(autocompleteInteraction.Data.Current.Value.ToString(), out uint result))
                {
                    var closestPowerOf2 = System.Numerics.BitOperations.RoundUpToPowerOf2(result);
                    Log.Debug("power of two: {power}", closestPowerOf2);
                    results.Add(new AutocompleteResult(closestPowerOf2.ToString(), closestPowerOf2));
                    return AutocompletionResult.FromSuccess(results);
                }
                else
                {
                    return AutocompletionResult.FromSuccess();
                }
            }
        }

        public class SaveGamesAutoCompleteHandler : AutocompleteHandler
        {
            public string RunningPath { get; set; }
            public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services)
            {
                var results = new List<AutocompleteResult>();

                if (!Directory.Exists(Path.Combine(RunningPath, "saves"))) return AutocompletionResult.FromSuccess();

                var fichiers = Directory.GetFiles(Path.Combine(RunningPath, "saves"));

                foreach (var fichier in fichiers)
                {
                    var name = Path.GetFileName(fichier);
                    results.Add(new AutocompleteResult(name, name));
                }

                return AutocompletionResult.FromSuccess(results.Take(25));
            }
        }
    }

    public class HaveGameInitializedForFucksSake : PreconditionAttribute
    {
        public override string ErrorMessage { get; } = "My brother o jogo não está iniciado";
        public async override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
        {
            if (GameMgMt.CurrentlyPlaying == null) return PreconditionResult.FromError("My brother o jogo não está iniciado");
            else return PreconditionResult.FromSuccess();
        }
    }
}
