// src/app/services/game.service.ts
import { Injectable, signal, WritableSignal } from '@angular/core';
import * as signalR from '@microsoft/signalr'; // Importa o pacote SignalR

@Injectable({
  // 'providedIn: 'root'' torna o serviço um singleton global,
  // disponível para injeção em qualquer lugar da aplicação.
  providedIn: 'root'
})
export class GameService {
  // A URL base da API do backend para o hub SignalR.
  // Certifique-se de que a porta corresponde à sua configuração do backend.
  private hubUrl: string = 'http://localhost:5123/gamehub';
  private hubConnection: signalR.HubConnection | undefined;

  // Signal do Angular para armazenar e expor as mensagens do chat em tempo real.
  // WritableSignal<string[]> permite que o serviço atualize a lista de mensagens.
  public chatMessages: WritableSignal<string[]> = signal<string[]>([]);

  constructor() {
    // No construtor, podemos inicializar a conexão ou fazer isso sob demanda.
    // Para este exemplo, a conexão será iniciada explicitamente pelo componente.
  }

  /**
   * Inicia a conexão com o hub SignalR.
   * Configura reconexão automática em caso de desconexão.
   */
  public async connect(): Promise<void> {
    if (this.hubConnection && this.hubConnection.state === signalR.HubConnectionState.Connected) {
      console.log('Já conectado ao hub.');
      return;
    }

    // Constrói a conexão com o hub, permitindo credenciais (para CORS e autenticação futura).
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(this.hubUrl, { withCredentials: true })
      .withAutomaticReconnect() // Configura a reconexão automática.
      .build();

    // Configura os listeners para os eventos do hub ANTES de iniciar a conexão.
    this.setupHubListeners();

    try {
      await this.hubConnection.start();
      console.log('Conexão SignalR iniciada com sucesso.');
    } catch (error) {
      console.error('Erro ao iniciar a conexão SignalR:', error);
      // Implementar lógica de retry ou notificação ao usuário em caso de falha.
    }
  }

  /**
   * Configura os listeners para os eventos enviados pelo GameHub do backend.
   * Este método deve ser chamado antes de iniciar a conexão.
   */
  private setupHubListeners(): void {
    if (!this.hubConnection) {
      console.error('HubConnection não inicializada.');
      return;
    }

    // Listener para o evento 'ReceberMensagemTeste' vindo do backend.
    // O backend enviará 'nomeUsuario' e 'mensagem'.
    this.hubConnection.on('ReceberMensagemTeste', (nomeUsuario: string, mensagem: string) => {
      // Atualiza o signal com a nova mensagem.
      // Usa 'update' para adicionar a nova mensagem à lista existente, mantendo a reatividade.
      this.chatMessages.update(messages => [...messages, `${nomeUsuario}: ${mensagem}`]);
      console.log(`Mensagem recebida: ${nomeUsuario}: ${mensagem}`);
    });

    // Opcional: Adicionar listeners para eventos de estado da conexão.
    this.hubConnection.onreconnecting(error => {
      console.warn('Reconectando ao SignalR...', error);
    });

    this.hubConnection.onreconnected(connectionId => {
      console.log('Reconexão SignalR bem-sucedida. Novo ConnectionId:', connectionId);
      // Pode ser necessário re-entrar na sala após a reconexão, dependendo da lógica do jogo.
      // Para este chat de teste, o backend gerencia o grupo por ConnectionId.
    });

    this.hubConnection.onclose(error => {
      console.error('Conexão SignalR fechada:', error);
      // Notificar o usuário ou tentar reconectar.
    });
  }

  /**
   * Desconecta do hub SignalR.
   */
  public async disconnect(): Promise<void> {
    if (this.hubConnection) {
      await this.hubConnection.stop();
      console.log('Conexão SignalR desconectada.');
      this.hubConnection = undefined; // Limpa a referência.
      this.chatMessages.set([]); // Limpa as mensagens ao desconectar.
    }
  }

  /**
   * Envia uma chamada para o método 'EntrarNaSala' no GameHub do backend.
   * @param codigoSala O código da sala para entrar.
   * @param nomeUsuario O nome do usuário que está entrando.
   */
  public async entrarNaSala(codigoSala: string, nomeUsuario: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.error('Não conectado ao hub SignalR.');
      return;
    }
    try {
      // Invoca o método do servidor. Os argumentos devem corresponder à assinatura do método no Hub.
      await this.hubConnection.invoke('EntrarNaSala', codigoSala, nomeUsuario);
      console.log(`Entrou na sala '${codigoSala}' como '${nomeUsuario}'.`);
      this.chatMessages.update(messages => [...messages, `Você entrou na sala '${codigoSala}'.`]);
    } catch (error) {
      console.error('Erro ao tentar entrar na sala:', error);
    }
  }

  /**
   * Envia uma chamada para o método 'EnviarMensagemTeste' no GameHub do backend.
   * @param codigoSala O código da sala.
   * @param mensagem A mensagem a ser enviada.
   */
  public async enviarMensagemTeste(codigoSala: string, nomeUsuario: string, mensagem: string): Promise<void> {
    if (!this.hubConnection || this.hubConnection.state !== signalR.HubConnectionState.Connected) {
      console.error('Não conectado ao hub SignalR.');
      return;
    }
    try {
      // Invoca o método do servidor. Os argumentos devem corresponder à assinatura do método no Hub.
      await this.hubConnection.invoke('EnviarMensagemTeste', codigoSala, nomeUsuario, mensagem);
      console.log(`Mensagem '${mensagem}' enviada para a sala '${codigoSala}'.`);
    } catch (error) {
      console.error('Erro ao tentar enviar mensagem de teste:', error);
    }
  }
}