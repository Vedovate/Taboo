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
- **Código morto**: Remova arquivos não utilizados (ex.: `app.html` placeholder, `app.module.ts` vazio, componentes sem rota)

## 6. Estrutura do Projeto
- **Backend**: `Tipoo.Api/` - API baseada em SignalR (pode conter rest se for explicitamente solicitado)
- **Frontend**: `tipoo-client/` - Angular 22 com Standalone Components e Signals
- **Testes Backend**: `api/tests/Tipoo.Api.Tests/` - xUnit + Moq
- **Testes Frontend**: `tipoo-client/src/**/*.spec.ts` - Vitest via Angular builder
- **Solution**: `Tipoo.sln` - Inclui projetos principal e de testes
