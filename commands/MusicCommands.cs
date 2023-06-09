using Discord.Interactions;
using Discord;
using Serilog;
using Newtonsoft.Json;
using System.Text;

namespace vim.music.commands
{
    [Group("music", "Comandos relacionados ao gerenciamento de músicas")]
    public class MusicCommands : InteractionModuleBase<SocketInteractionContext>
    {
        public jsonstructs.Configuration Config { get; set; }
        public string RunningPath { get; set; }

        [SlashCommand("registrar", "Registrar uma música no VIMMUSICA")]
        public async Task Command()
        {
            await Context.Interaction.RespondWithModalAsync<RegisterModal>("register_music_modal");
        }

        public class RegisterModal : IModal
        {
            public string Title => "Registrar música";

            [InputLabel("Nome do jogo")]
            [ModalTextInput("game_name", TextInputStyle.Short, "Silent Hill 2", 1, 71)]
            public string GameName { get; set; }

            [InputLabel("Nome da música")]
            [ModalTextInput("music_name", TextInputStyle.Short, "Theme of Laura", 1, 71)]
            public string MusicName { get; set; }

            [InputLabel("Link da música")]
            [ModalTextInput("music_link", TextInputStyle.Short, "https://www.youtube.com/watch?v=6LB7LZZGpkw", 1)]
            public string MusicLink { get; set; }
        }

        [ModalInteraction("register_music_modal", ignoreGroupNames: true)]
        public async Task MusicModalHandle(RegisterModal modal)
        {
            string datafile = Path.Combine(RunningPath, "pre_musics.json");

            string jogo = Ser(modal.GameName);

            Log.Information("{user} respondeu o formulário e selecionou o jogo: {jogo} a música: {musica}", Context.Interaction.User.Username, modal.GameName, modal.MusicName);

            if (!File.Exists(datafile))
            {
                Log.Warning("Arquivo pre_musica não existente, criando...");
                File.WriteAllText(datafile, JsonConvert.SerializeObject(new jsonstructs.DataSet()));
            }

            string json = File.ReadAllText(datafile);
            jsonstructs.DataSet dataset = JsonConvert.DeserializeObject<jsonstructs.DataSet>(json);

            if (dataset.entries.Where(x => x.Jogo == jogo).ToArray().Length == 2)
            {
                var embed = new EmbedBuilder().WithTitle("Erro").WithDescription("O jogo desejado já tem duas músicas registradas.").WithImageUrl("https://i.imgur.com/R8F5Iuy.png").WithColor(Discord.Color.Red).Build();
                await Context.Interaction.RespondAsync(embed: embed, ephemeral: true);

                Log.Information("{user} teve o seu formulário recusado por já haverem duas música do jogo requisitado", Context.Interaction.User.Username);

                return;
            }

            if (dataset.entries.Where(x => x.userId == Context.Interaction.User.Id).ToArray().Length >= Config.MaxFormAwnsersByPerson && Config.MaxFormAwnsersByPerson != -1)
            {
                var embed = new EmbedBuilder().WithTitle("Erro").WithDescription("Apenas uma pessoa pode responder esse formulário").WithColor(Discord.Color.Red).Build();
                await Context.Interaction.RespondAsync(embed: embed, ephemeral: true);

                Log.Information("{user} tentou enviar o formulário mais de uma vez", Context.Interaction.User.Username);

                return;
            }

            if (Uri.IsWellFormedUriString(modal.MusicLink, UriKind.Absolute))
            {
                var embed = new EmbedBuilder().WithTitle("Erro").WithDescription("O seu link não parece ser válido").WithColor(Discord.Color.Red).Build();
                await Context.Interaction.RespondAsync(embed: embed, ephemeral: true);

                Log.Information("{user} teve o seu formulário recusado por não apresentar um link válido", Context.Interaction.User.Username);

                return;
            }

            if (dataset.entries.Where(x => x.MusicNameSerialized == Ser(modal.MusicName)).ToArray().Length > 0)
            {
                var embed = new EmbedBuilder().WithTitle("Erro").WithDescription("Essa música já foi registrada").WithColor(Discord.Color.Red).Build();
                await Context.Interaction.RespondAsync(embed: embed, ephemeral: true);

                Log.Information("{user} teve o seu formulário recusado pela música já estar registrada, {music}", Context.Interaction.User.Username, modal.MusicName);
            }

            var datareg = new jsonstructs.DataRegistry()
            {
                Jogo = jogo,
                MusicName = modal.MusicName,
                MusicLink = modal.MusicLink,
                userId = Context.Interaction.User.Id,
                MusicNameSerialized = Ser(modal.MusicName)
            };

            dataset.entries.Add(datareg);

            string jsonE = JsonConvert.SerializeObject(dataset, Formatting.Indented);
            File.WriteAllText(datafile, jsonE);

            var aembed = new EmbedBuilder().WithTitle("Sucesso").WithDescription("Formulário recebido com sucesso").WithColor(Color.Green);
            await Context.Interaction.RespondAsync(embed: aembed.Build(), ephemeral: true);
        }

        private string Ser(string input)
        {
            input = input.ToLowerInvariant().Trim().Replace(" ", "_").Normalize();

            string stFormD = input.Normalize(NormalizationForm.FormD);
            int len = stFormD.Length;
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < len; i++)
            {
                System.Globalization.UnicodeCategory uc = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stFormD[i]);
                if (uc != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[i]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }
    }
}