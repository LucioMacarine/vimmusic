dotnet restore
dotnet run

raíz do executavel = /bin/Debug/net7.0/(sistema operacional ou sla)/

na raíz config.json:

<img src="https://i.imgur.com/WV3z7T1.png"></img>
a config deve ser gerada automaticamente qnd vc ligar o bot

MaxFormAwnserrsByPerson: quantas vezes uma única pessoa pode responder o formulário (-1 para infinitas)
timer:
  enabled: se verdadeiro habilida o fim do voto automaticamente
  timeUntilVoteClosesMS: se acima for verdadeiro define o tempo em milisegundos pra terminar o voto automaticamente
token: auto-explicativo

resumo dos comandos:
/music registrar : abre o formulário para submeter uma música (o comando pro povão)
/game start : inicia um novo jogo
/game vote : inicia uma votação contra duas músicas (1x1)
/game vote_end : encerra manualmente a votação (lembrando que o sistema de encerrar automáticamente basicamente só executa isso)
/game status : mostra o status da partida (não os duelos individuais mas todos do nível)
/game progress_game : usar no fim de um nível para passar para o próximo com os ganhadores
/game load : carrega um arquivo de save (o jogo é salvo em todo fim de nível)
