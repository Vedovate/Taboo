# DIRETRIZES GERAIS DE FLUXO DE TRABALHO E TESTES OBRIGATÓRIOS

Você opera em um repositório que contém um backend (API C#) no diretório `Tipoo.Api/` e um frontend (Angular 22 utilizing Vitest) no diretório `tipoo-client/`. 

A sua regra primária de operação é: **Nenhum código pode ser commitado, finalizado ou propagado sem que a suíte de testes seja executada e aprovada.**

## 1. Fluxo de Modificação de Código
- Ao criar uma nova regra de negócio, componente ou serviço, **escreva os testes correspondentes imediatamente**.
- Ao alterar um comportamento existente, **atualize os testes** para refletir a nova regra.
- Nunca comente, desative ou ignore um teste que falhou para forçar uma aprovação.

## 2. Protocolo de Execução de Testes
Sempre que finalizar a escrita ou modificação do código, execute os testes da camada afetada antes de preparar qualquer commit.

**Para o Frontend (`tipoo-client/`):**
1. Acesse o diretório: `cd tipoo-client`
2. Execute o Vitest em modo de execução única (non-watch mode): `npm run test`
3. Analise o output do terminal.

**Para a API (`Tipoo.Api/`):**
1. Execute da raiz do repositório: `dotnet test`
2. Analise o output do terminal.

*Nota: Se a alteração for full-stack, execute os comandos de ambas as pastas sequencialmente.*

## 3. Ciclo Autônomo de Correção (Self-Healing)
Caso qualquer teste falhe:
1. **Não interrompa o processo** para pedir permissão ou aguardar input humano.
2. Analise a stack trace e o erro apontado pelo terminal.
3. Aplique a correção necessária no código da aplicação (se for um bug introduzido) ou no teste (se for uma mudança de contrato/comportamento esperada).
4. Re-execute o comando de teste na pasta correspondente.
5. Repita o ciclo até obter 100% de sucesso na suíte executada.

## 4. Proibição de Commit
Você está **PROIBIDO** de executar `git add`, `git commit`, `git push` ou qualquer operação que escreva no histórico do Git sem autorização expressa e explícita do usuário. Aguarde instruções claras antes de qualquer commit. 

## 5. Convenções Obrigatórias

### Backend (C# .NET 9 + SignalR)

- **DI**: Sempre registre interfaces, não classes concretas. Ex.: `AddSingleton<IGameManager, GameManager>()`
- **DTOs**: Hub methods nunca devem retornar anonymous types. Crie `PlayerDto` e similares em `Tipoo.Api/DTOs/`
- **Logging**: Injete `ILogger<T>` em todo service e hub. Em testes use `NullLogger<T>.Instance`
- **Error Handling**: Use `IHubFilter` global (em `Tipoo.Api/Filters/`) para capturar exceções não tratadas nos hubs
- **SQL Scripts**: Arquivos `.sql` devem ser Embedded Resource no `.csproj`, lidos via `Assembly.GetManifestResourceStream()` — nunca por caminho de arquivo
- **Connection Strings**: O `Program.cs` deve resolver o path absoluto; a string de configuração contém só o nome do arquivo (`Data Source=TipooGame.db`)

### Frontend (Angular 22 + Signals + Vitest)

- **Environment**: URLs de API/hubs nunca hardcoded. Sempre usar `src/environments/environment.ts`
- **Modelos**: Interfaces de domínio em `src/app/models/`, nunca inline em services
- **Reatividade**: Estado local de componentes deve usar `signal()` (nunca campos plain). Derived state use `computed()`. Leiam signals diretamente (`signal()`), nunca via getters síncronos
- **Pipe de tradução**: Usar `pure: true` com `effect()` e `ChangeDetectorRef.markForCheck()`, nunca `pure: false`
- **Selects em Angular**: Não usar `[value]` no `<select>` (não seleciona o option corretamente com `@for`); ligar `[selected]` em cada `<option>` (ex.: `[selected]="draft().startingTeam === t"`)
- **Código morto**: Remova arquivos não utilizados (ex.: `app.html` placeholder, `app.module.ts` vazio, componentes sem rota)

## 6. Estrutura do Projeto
- **Backend**: `Tipoo.Api/` - API baseada em SignalR (pode conter rest se for explicitamente solicitado)
- **Frontend**: `tipoo-client/` - Angular 22 com Standalone Components e Signals
- **Testes Backend**: `api/tests/Tipoo.Api.Tests/` - xUnit + Moq
- **Testes Frontend**: `tipoo-client/src/**/*.spec.ts` - Vitest via Angular builder
- **Solution**: `Tipoo.sln` - Inclui projetos principal e de testes
- **Modal de configs da partida**: `tipoo-client/src/app/components/match-settings-modal/` (aberto pelo lobby via botão "Configurações da Partida"; Salvar chama `configurarPartida` — persistência no cache do host `GameHostSettings`; não-host abre read-only; ao salvar mostra feedback de sucesso/erro `SETTINGS.SALVO`/`SETTINGS.ERRO`). Regras: dificuldades ordenadas Fácil→Médio→Difícil e não podem ser desmarcadas por completo; `buzzerSounds` pode ficar vazio (jogadores usam buzina própria) com aviso `SETTINGS.BUZZER_WARNING`; sliders exibem `Selecionado: <valor>`; `categories` foi **removido das configurações** (front `GameSettings` e back `GameSettings.cs`) mas permanece nos dados de carta/banco (`Card.Category`, `CartasOpcoesDto`, `init.sql`). Padrões/limites (back `GameSettings.cs` + front `createDefaultGameSettings()`): round time padrão 180s/máx 600s; rounds padrão 6; pausa entre rodadas slider 15s–300s padrão 30s; desempate (`TiebreakMode`) padrão `empatado`. **Validação por campo** (front): limites numéricos enforced em código via `LIMITES` (`numberOfRounds` 2–20 par, `skipLimit` 0–10, `tipooLeadLimit` 10–999999, pontos 0–10); `maxlength` **não funciona** em `input[type=number]`; campos vazios/fora do limite não atualizam o draft, mostram erro `SETTINGS.ERRORS.*`, Salvar fica `[disabled]` com erro, e `onBlurNumero` restaura o último valor válido ao sair do campo.
- **Tabela `Matches`** (`Database/init.sql`): registra partidas p/ estatística com chave = código da sala + data/hora de início. É criada **só ao iniciar** a partida (`GameManager.RegistrarInicioDePartida` chamado por `ForcarIniciar`, 1x por sala) e atualizada ao encerrar (UPDATE ainda não implementado — sem fluxo de fim de jogo)

## 7. Atualização de Memória (comando obrigatório)
Ao final de cada tarefa concluída (com testes aprovados), revise se houve aprendizados importantes sobre o sistema que valham ser registrados na memória (`AGENTS.md`). Siga estas regras:
1. Atualize **somente** quando houver algo relevante e durável (arquitetura, convenções, fluxos, decisões de design, comandos não triviais, contratos entre backend/frontend).
2. Mantenha o texto **resumido** (poucas linhas, tópicos curtos) para o arquivo não crescer descontroladamente.
3. **Não é obrigatório** adicionar a cada tarefa — pule quando o trabalho for trivial ou repetitivo.
4. Releia o `AGENTS.md` antes de editar para evitar duplicações; edite por substituição (ex.: agregue em seções existentes como convenções ou estrutura do projeto).
