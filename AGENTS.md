# DIRETRIZES GERAIS DE FLUXO DE TRABALHO E TESTES OBRIGATÓRIOS

Você opera em um repositório que contém um backend (API C#) no diretório `Taboo.Api/` e um frontend (Angular 22 utilizing Vitest) no diretório `taboo-client/`. 

A sua regra primária de operação é: **Nenhum código pode ser commitado, finalizado ou propagado sem que a suíte de testes seja executada e aprovada.**

## 1. Fluxo de Modificação de Código
- Ao criar uma nova regra de negócio, componente ou serviço, **escreva os testes correspondentes imediatamente**.
- Ao alterar um comportamento existente, **atualize os testes** para refletir a nova regra.
- Nunca comente, desative ou ignore um teste que falhou para forçar uma aprovação.

## 2. Protocolo de Execução de Testes
Sempre que finalizar a escrita ou modificação do código, execute os testes da camada afetada antes de preparar qualquer commit.

**Para o Frontend (`taboo-client/`):**
1. Acesse o diretório: `cd taboo-client`
2. Execute o Vitest em modo de execução única (non-watch mode): `npm run test`
3. Analise o output do terminal.

**Para a API (`Taboo.Api/`):**
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

## 4. Autorização de Commit
Você possui autorização para rodar `git add` e `git commit` **apenas** quando o terminal confirmar que todos os testes passaram. Na mensagem de commit, inclua uma linha ao final atestando a integridade (ex: `[Testes: Aprovados em taboo-client e/ou Taboo.Api]`). 

## 5. Estrutura do Projeto
- **Backend**: `Taboo.Api/` - API baseada em SignalR (não REST)
- **Frontend**: `taboo-client/` - Angular 22 com Standalone Components e Signals
- **Testes Backend**: `api/tests/Taboo.Api.Tests/` - xUnit + Moq
- **Testes Frontend**: `taboo-client/src/**/*.spec.ts` - Vitest via Angular builder
- **Solution**: `Taboo.sln` - Inclui projetos principal e de testes
