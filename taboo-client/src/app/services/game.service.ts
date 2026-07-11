import { Injectable, signal, computed } from '@angular/core';
import { Router } from '@angular/router';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from '../../environments/environment';
import { LobbyPlayer } from '../models/lobby-player';

@Injectable({ providedIn: 'root' })
export class GameService {
  constructor(private router: Router) {}
  private readonly hubUrl = environment.signalR.hubUrl;
  private hubConnection?: HubConnection;
  private currentRoomCode = '';
  private userName = '';
  private errorTimeout: ReturnType<typeof setTimeout> | null = null;
  private errorCountdownInterval: ReturnType<typeof setInterval> | null = null;

  readonly error = signal<string>('');
  readonly errorTimeLeft = signal(0);
  readonly isRoomFull = signal(false);

  readonly messages = signal<string[]>([]);
  readonly connected = signal(false);
  readonly roomCode = signal('');
  readonly players = signal<LobbyPlayer[]>([]);
  readonly playerCount = computed(() => this.players().length);
  readonly meuConnectionId = signal('');
  readonly nomeFinalizado = computed(() => {
    const eu = this.players().find(p => p.connectionId === this.meuConnectionId());
    return eu ? eu.name !== eu.connectionId : false;
  });

  async conectar(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.setError('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    this.clearError();
    this.isRoomFull.set(false);
    this.connected.set(false);
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }
      this.meuConnectionId.set(connection.connectionId ?? '');

      const resultado = await connection.invoke<boolean>('EntrarNaSala', sala, usuario);
      if (!resultado) {
        if (!this.isRoomFull()) {
          this.setError('Sala não encontrada. Verifique o código e tente novamente.');
        }
        return;
      }
      this.connected.set(true);
    } catch (error: any) {
      console.error('Erro ao conectar:', error);
      const errorMsg = error?.message || 'Ocorreu um erro desconhecido.';
      if (!errorMsg.includes('already connected')) {
        this.setError(errorMsg);
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
      this.meuConnectionId.set('');
      
      this.error.set('');
      this.errorTimeLeft.set(0);
    } catch (error: any) {
      console.error('Erro ao desconectar:', error);
    }
  }

  async alterarNome(novoNome: string): Promise<void> {
    if (!this.hubConnection || !this.currentRoomCode) {
      return;
    }
    await this.hubConnection.invoke('AlterarNome', this.currentRoomCode, novoNome);
  }

  async expulsarJogador(connectionIdAlvo: string): Promise<void> {
    if (!this.hubConnection || !this.currentRoomCode) {
      return;
    }
    await this.hubConnection.invoke('ExpulsarJogador', this.currentRoomCode, connectionIdAlvo);
  }

  async createRoom(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.setError('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    this.clearError();
    this.currentRoomCode = sala;
    this.roomCode.set(sala);
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    try {
      if (connection.state !== HubConnectionState.Connected) {
        await connection.start();
      }
      this.meuConnectionId.set(connection.connectionId ?? '');

      const resultado = await connection.invoke<boolean>('CriarSala', sala, usuario);
      if (!resultado) {
        this.setError('Já existe uma sala com esse código. Tente novamente.');
        return;
      }
      this.connected.set(true);
    } catch (error: any) {
      console.error('Erro ao criar sala:', error);
      this.setError(error?.message || 'Ocorreu um erro ao criar a sala.');
    }
  }

  setError(msg: string): void {
    this.clearError();
    this.error.set(msg);
    this.errorTimeLeft.set(8);
    this.errorCountdownInterval = setInterval(() => {
      this.errorTimeLeft.update(v => {
        if (v <= 1) {
          this.error.set('');
          this.errorTimeLeft.set(0);
          if (this.errorCountdownInterval) {
            clearInterval(this.errorCountdownInterval);
            this.errorCountdownInterval = null;
          }
          return 0;
        }
        return v - 1;
      });
    }, 1000);
    this.errorTimeout = setTimeout(() => {
      if (this.errorCountdownInterval) {
        clearInterval(this.errorCountdownInterval);
        this.errorCountdownInterval = null;
      }
    }, 8000);
  }

  clearError(): void {
    this.error.set('');
    this.errorTimeLeft.set(0);
    if (this.errorTimeout) {
      clearTimeout(this.errorTimeout);
      this.errorTimeout = null;
    }
    if (this.errorCountdownInterval) {
      clearInterval(this.errorCountdownInterval);
      this.errorCountdownInterval = null;
    }
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
      const eu = players.find(p => p.connectionId === this.meuConnectionId());
      if (eu && eu.name === this.meuConnectionId()) {
        const novoNome = `Jogador ${players.length}`;
        this.alterarNome(novoNome);
      }
    });

    this.hubConnection.on('SalaCheia', (message: string) => {
      this.isRoomFull.set(true);
      this.setError(message);
    });

    this.hubConnection.on('FoiExpulso', () => {
      this.desconectar();
      this.router.navigate(['/']);
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
