# Tipoo

Jogo de navegador inspirado no Taboo: descreva a palavra da carta sem usar as palavras proibidas e faça seu time pontuar.

## Arquitetura

| Parte | Stack | Pasta |
| --- | --- | --- |
| Backend | ASP.NET Core 9 + SignalR + SQLite | `Tipoo.Api/` |
| Frontend | Angular 22 (Standalone Components + Signals) | `tipoo-client/` |

A comunicação em tempo real é feita via SignalR (`/gamehub`). O banco é um arquivo SQLite local, inicializado automaticamente no startup da API a partir do script embutido `Tipoo.Api/Database/init.sql`.

## Como rodar

### Backend
```bash
cd Tipoo.Api
dotnet run
```
A API sobe em `http://localhost:5000` (ou `https://localhost:5001`). No startup, o `DbInitializer` executa o `init.sql` para criar as tabelas e popular as cartas (se ainda não existirem).

### Frontend
```bash
cd tipoo-client
npm install
npm start   # ou ng serve
```
Abra `http://localhost:4200`.

### Testes
```bash
dotnet test                       # testes da API (xUnit + Moq)
cd tipoo-client && npm run test   # testes do frontend (Vitest)
```

> **Importante:** os scripts `.sql` do banco (que contêm os dados das cartas) estão no `.gitignore` para não vazar o conteúdo do jogo. Eles precisam existir localmente para o build/startup — em um clone novo, recrie o `Tipoo.Api/Database/init.sql` (veja a seção de tabelas) antes de rodar.

## Banco de dados

O SQLite é inicializado a partir do `init.sql` (Embedded Resource, lido via `Assembly.GetManifestResourceStream`). Todas as tabelas usam `CREATE TABLE IF NOT EXISTS` e os inserts usam `INSERT OR IGNORE`, então o script é idempotente.

### `Cards`
As cartas do jogo.

| Coluna | Tipo | Descrição |
| --- | --- | --- |
| `Id` | INTEGER PK | Identificador (auto incremento) |
| `MainWord` | TEXT | Palavra principal (exibida ao explicador) |
| `Forbidden1`–`Forbidden5` | TEXT | As 5 palavras proibidas |
| `Difficulty` | TEXT | `Fácil`, `Médio` ou `Difícil` |
| `Category` | TEXT | Categoria temática (ex.: `Objeto`, `Alimento`, `Animal`) |

### `GameHostHistory`
Cache do host: cartas já usadas em partidas anteriores (evita repetir na mesma sessão do navegador do host).

| Coluna | Tipo | Descrição |
| --- | --- | --- |
| `HostSessionId` | TEXT | Identificador da sessão do host (navegador) |
| `CardId` | INTEGER | FK → `Cards.Id` |
| `CreatedAt` | DATETIME | Quando a carta foi usada |

PK composta: `(HostSessionId, CardId)`. `ON DELETE CASCADE` com `Cards`.

### `GameHostSettings`
Cache das configurações da partida por navegador do host.

| Coluna | Tipo | Descrição |
| --- | --- | --- |
| `HostSessionId` | TEXT PK | Sessão do host |
| `SettingsJson` | TEXT | Configurações serializadas (JSON) |
| `UpdatedAt` | DATETIME | Última atualização |

### `Matches`
Registro de partidas para estatísticas. A chave é o código da sala + data/hora de início. O registro é criado ao **iniciar** a partida e atualizado ao **encerrar**.

| Coluna | Tipo | Descrição |
| --- | --- | --- |
| `Id` | INTEGER PK | Auto incremento |
| `MatchKey` | TEXT UNIQUE | Código da sala + data/hora de início |
| `RoomCode` | TEXT | Código da sala |
| `HostSessionId` | TEXT | Sessão do host que iniciou |
| `StartedAt` | DATETIME | Início da partida |
| `SettingsJson` | TEXT | Configurações usadas |
| `StartedPlayers` | INTEGER | Jogadores no início |
| `WasStarted` / `Completed` | INTEGER | Flags de estado |
| `EndedAt`, `FinishedPlayers` | DATETIME/INTEGER | Fim da partida |
| `FinalScoreRed` / `FinalScoreBlue` / `WinnerTeam` | — | Resultado final |

## Regras do jogo

1. **Partida:** duas equipes (Vermelho × Azul). Uma partida tem `numberOfRounds` rodadas (padrão 6, sempre par, entre 2 e 20), com pausa entre rodadas (padrão 30s, 15s–300s).
2. **Time inicial:** `startingTeam` (padrão `aleatorio`, sorteado ao iniciar; pode ser fixado em vermelho/azul).
3. **Rodada:** o explicador do time da vez vê a carta (`MainWord` + 5 proibidas) e precisa fazer o time acertar sem dizer nenhuma palavra proibida.
4. **Tempo:** cada rodada tem `roundTimeSeconds` (padrão 180s, clamp 30s–600s). O modo pânico (`panicMode`) esconde o timer visual.
5. **Pontuação:** acerto soma `pointsPerCorrect`; erro desconta `pointsPerError`. O explicador pode pular a carta: `skipLimit` pulos por rodada (padrão 3, 0–10); o pulo desconta `pointsPerSkip` apenas se `skipCostsPoints` estiver ativo.
6. **Dificuldade:** as configurações permitem escolher as dificuldades (`Fácil`, `Médio`, `Difícil`) usadas no sorteio das cartas.
7. **Empate:** `tiebreakMode` define o critério em caso de empate no fim (`empatado` = empate permanece; `rodada-extra` = desempate).
8. **Fim:** o placar acumulado Vermelho × Azul aparece nas telas de fim de rodada/partida; o resultado é registrado na tabela `Matches`.

## Estrutura

```
Tipoo.Api/                 Backend (SignalR + SQLite)
  Database/init.sql        Schema + dados das cartas (gitignored)
  Data/                    GameDataStore (Dapper + SQLite)
  Hubs/GameHub.cs          Comunicação em tempo real
  Models/                  Card, GameSettings, GameMatch
  Services/GameManager.cs  Regras da partida
tipoo-client/              Frontend Angular 22
  src/app/                 Componentes standalone + services + signals
api/tests/Tipoo.Api.Tests/ Testes da API (xUnit + Moq)
```
