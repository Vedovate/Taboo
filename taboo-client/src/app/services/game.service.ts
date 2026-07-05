import { Injectable, signal, computed } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { LobbyPlayer } from '../models/lobby-player';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly hubUrl = environment.signalR.hubUrl;
  private hubConnection?: HubConnection;
  private currentRoomCode = '';
  private userName = '';
  
  // Sinal para armazenar mensagens de erro temporárias
  readonly error = signal<string>('');

  readonly messages = signal<string[]>([]);
  readonly connected = signal(false);
  readonly roomCode = signal('');
  readonly players = signal<LobbyPlayer[]>([]);
  readonly playerCount = computed(() => this.players().length);

  async conectar(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.error.set('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    // Limpa erro anterior ao tentar conectar
    this.error.set('');
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }

      const resultado = await connection.invoke<boolean>('EntrarNaSala', sala, usuario);
      if (!resultado) {
        this.error.set('Sala não encontrada. Verifique o código e tente novamente.');
        return;
      }
      this.connected.set(true);
      this.players.set([{ name: usuario, isHost: false }]);
    } catch (error: any) {
      console.error('Erro ao conectar:', error);
      const errorMsg = error?.message || 'Ocorreu um erro desconhecido.';
      if (!errorMsg.includes('already connected')) {
        this.error.set(errorMsg);
      } else {
        this.connected.set(true);
      }
    }
  }

  async enviarMensagem(mensagem: string): Promise<void> {
    const texto = mensagem.trim();

    if (!texto || !this.hubConnection || this.hubConnection.state !== HubConnectionState.Connected) {
      return;
    }

    await this.hubConnection.invoke('EnviarMensagem', this.currentRoomCode, texto);
  }

  async desconectar(): Promise<void> {
    try {
      if (this.hubConnection) {
        await this.hubConnection.stop();
        this.hubConnection = undefined;
      }

      this.currentRoomCode = '';
      this.roomCode.set('');
      this.userName = '';
      this.connected.set(false);
      this.players.set([]);
      this.messages.set([]);
      
      // Limpa erro ao desconectar com sucesso
      this.error.set('');
    } catch (error: any) {
      console.error('Erro ao desconectar:', error);
    }
  }

  async createRoom(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.error.set('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    this.error.set('');
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }

      const resultado = await connection.invoke<boolean>('CriarSala', sala, usuario);
      if (!resultado) {
        this.error.set('Já existe uma sala com esse código. Tente novamente.');
        return;
      }
      this.connected.set(true);
      this.players.set([{ name: usuario, isHost: true }]);
    } catch (error: any) {
      console.error('Erro ao criar sala:', error);
      this.error.set(error?.message || 'Ocorreu um erro ao criar a sala.');
    }
  }

  clearError(): void {
    this.error.set('');
  }

  private getOrCreateConnection(): HubConnection {
    if (this.hubConnection) {
      return this.hubConnection;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.registerHandlers();
    return this.hubConnection;
  }

  private registerHandlers(): void {
    if (!this.hubConnection) {
      return;
    }

    this.hubConnection.on('ReceberMensagem', (mensagem: string) => {
      this.messages.update(current => [...current, mensagem]);
    });

    this.hubConnection.on('AtualizarJogadores', (players: LobbyPlayer[]) => {
      this.players.set(players);
    });

    this.hubConnection.onreconnecting(() => {
      this.connected.set(false);
    });

    this.hubConnection.onreconnected(async () => {
      this.connected.set(true);

      if (this.currentRoomCode && this.userName) {
        await this.hubConnection?.invoke('EntrarNaSala', this.currentRoomCode, this.userName);
      }
    });

    this.hubConnection.onclose(() => {
      if (!this.connected()) {
        this.connected.set(false);
        this.messages.set([]);
        setTimeout(() => {
          this.error.set('');
        }, 2000);
      }
    });
  }
}
