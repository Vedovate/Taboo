// src/app/services/game.service.ts
import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

@Injectable({ providedIn: 'root' })
export class GameService {
  private readonly hubUrl = 'http://localhost:5123/gamehub';
  private hubConnection?: HubConnection;
  private roomCode = '';
  private userName = '';
  
  // Sinal para armazenar mensagens de erro temporárias
  readonly error = signal<string>('');

  readonly messages = signal<string[]>([]);
  readonly connected = signal(false);

  async conectar(codigoSala: string, nomeUsuario: string): Promise<void> {
    const sala = codigoSala.trim();
    const usuario = nomeUsuario.trim();

    if (!sala || !usuario) {
      this.error.set('Por favor, preencha o código da sala e seu nome.');
      return;
    }

    // Limpa erro anterior ao tentar conectar
    this.error.set('');
    
    this.roomCode = sala;
    this.userName = usuario;
    this.messages.set([]);

    const connection = this.getOrCreateConnection();

    if (connection.state !== HubConnectionState.Connected) {
      await connection.start();
    }

    try {
      await connection.invoke('EntrarNaSala', sala, usuario);
      this.connected.set(true);
    } catch (error: any) {
      console.error('Erro ao conectar:', error);
      // Mantém o estado de conexão como false em caso de erro
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

    await this.hubConnection.invoke('EnviarMensagem', this.roomCode, texto);
  }

  async desconectar(): Promise<void> {
    try {
      if (this.hubConnection) {
        await this.hubConnection.stop();
        this.hubConnection = undefined;
      }

      this.roomCode = '';
      this.userName = '';
      this.connected.set(false);
      this.messages.set([]);
      
      // Limpa erro ao desconectar com sucesso
      this.error.set('');
    } catch (error: any) {
      console.error('Erro ao desconectar:', error);
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

    this.hubConnection.onreconnecting(() => {
      this.connected.set(false);
    });

    this.hubConnection.onreconnected(async () => {
      this.connected.set(true);

      if (this.roomCode && this.userName) {
        await this.hubConnection?.invoke('EntrarNaSala', this.roomCode, this.userName);
      }
    });

    this.hubConnection.onclose(() => {
      // Só limpa o estado se não estiver conectado em outra sala
      if (!this.connected()) {
        this.connected.set(false);
        this.messages.set([]);
        
        // Limpa erro apenas após um pequeno delay para permitir que a UI mostre mensagens de sucesso anteriores
        setTimeout(() => {
          this.error.set('');
        }, 2000);
      } else {
        // Usuário está em outra sala - não limpar o estado da conexão atual
        console.log('Conexão fechada, mas usuário ainda conectado na mesma sala.');
      }
    });
  }
}
