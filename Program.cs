using Microsoft.Extensions.DependencyInjection;
using Discord.Interactions;
using Discord.WebSocket;
using Discord;
using Serilog;
using Newtonsoft.Json;

namespace vim.music
{
    public class Program
    {
        private static readonly string RunningPath = AppDomain.CurrentDomain.BaseDirectory;

        public static void Main(string[] args)
        {
            MainAsync(args).GetAwaiter().GetResult();
        }

        public static async Task MainAsync(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
            // .MinimumLevel.Verbose()
            .WriteTo.Console()
            .CreateLogger();

            Log.Information("----- BENEVIENDO AO VAI PRA PUTA QUE PARIU DESGRAÇADOQWN EJIKW H	QUJioEJNQWBJIKOLEN1Q23EJ2n13 '1QEJ16WUIEJNUI16N7 UIO16NJI 1NUKOL FODA-SE ESSE PROJETO DE MERDA EU VOU ME MATAR PREFIRO MESMO FOSASE ESSE MERDA DESSE VIDA DO CARALHO -----");

            jsonstructs.Configuration config;
            var configPath = Path.Combine(RunningPath, "config.json");
            if (!File.Exists(configPath))
            {
                Log.Warning("config.json não encontrado, criando....");
                config = new jsonstructs.Configuration();
                File.WriteAllText(configPath, JsonConvert.SerializeObject(config, Formatting.Indented));
            }
            else config = JsonConvert.DeserializeObject<jsonstructs.Configuration>(File.ReadAllText(configPath)) ?? new jsonstructs.Configuration();
            Log.Information("Configuração carregada...");

            var services = new ServiceCollection()
            .AddSingleton(config)
            .AddSingleton(RunningPath)
            .BuildServiceProvider();

            var client = new DiscordSocketClient();
            client.Log += LogDiscordClientMessage;

            var interactions = new InteractionService(client, new InteractionServiceConfig()
            {
                WildCardExpression = "*"
            });
            await interactions.AddModulesAsync(System.Reflection.Assembly.GetExecutingAssembly(), services);

            Log.Information("Fazendo login...");
            await client.LoginAsync(TokenType.Bot, config.Token);
            await client.StartAsync();

            client.Ready += async () =>
            {
                Log.Information("Registrando comandos");
                await interactions.RegisterCommandsToGuildAsync((ulong)1104118276956618874, true);
            };

            client.InteractionCreated += async (interaction) =>
            {
                Log.Debug("Interação recebida");
                var ctx = new SocketInteractionContext(client, interaction);
                await interactions.ExecuteCommandAsync(ctx, services);
            };

            interactions.SlashCommandExecuted += async (SlashCommandInfo info, IInteractionContext context, IResult result) =>
            {
                if (result.Error == InteractionCommandError.UnmetPrecondition)
                {
                    var embed = new EmbedBuilder().WithColor(Color.Red).WithTitle("Erro").WithDescription(result.ErrorReason);
                    await context.Interaction.RespondAsync(embed: embed.Build(), ephemeral: true);
                    return;
                }

                if (!result.IsSuccess)
                {
                    Log.Error("{err}", result.ToString());
                }
            };

            // client.Ready += async () =>
            // {
            //     Log.Warning("Deletando comandos");
            //     var commands = await client.GetGlobalApplicationCommandsAsync();
            //     foreach (var command in commands)
            //     {
            //         await command.DeleteAsync();
            //     }
            // };

            await Task.Delay(-1);
        }

        private static async Task LogDiscordClientMessage(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Info:
                    Log.Information(message.Message);
                    break;
                case LogSeverity.Warning:
                    Log.Warning(message.Message);
                    break;
                case LogSeverity.Error:
                    Log.Error(message.Exception, message.Message);
                    break;
                case LogSeverity.Critical:
                    Log.Fatal(message.Exception, message.Message);
                    break;
                case LogSeverity.Verbose:
                    Log.Verbose(message.Message);
                    break;
                case LogSeverity.Debug:
                    Log.Debug(message.Message);
                    break;
            }
        }
    }
}