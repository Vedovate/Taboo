// src/app/components/chat-teste/chat-teste.component.ts
import { Component, inject, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common'; // Necessário para ngIf, ngFor
import { FormsModule } from '@angular/forms'; // Necessário para ngModel

import { GameService } from '../../services/game.service'; // Importa o nosso serviço

@Component({
  selector: 'app-chat-teste',
  standalone: true, // Componente standalone
  imports: [CommonModule, FormsModule], // Importa módulos necessários
  template: `
    <div class="chat-container">
      <h2>Chat de Teste SignalR</h2>

      <div class="controls">
        <label for="roomCode">Código da Sala:</label>
        <input id="roomCode" type="text" [(ngModel)]="roomCode" placeholder="Ex: SALA123" />

        <label for="userName">Seu Nome:</label>
        <input id="userName" type="text" [(ngModel)]="userName" placeholder="Ex: Jogador1" />

        <button (click)="joinRoom()" [disabled]="!roomCode || !userName || !isGameServiceConnected()">
          Entrar na Sala
        </button>
        <button (click)="disconnect()" [disabled]="!isGameServiceConnected()">
          Desconectar
        </button>
      </div>

      @if (gameService.chatMessages().length > 0) {
        <div class="messages-display">
          <h3>Mensagens da Sala:</h3>
          @for (message of gameService.chatMessages(); track $index) {
            <p>{{ message }}</p>
          }
        </div>
      }

      <div class="message-input">
        <label for="messageText">Mensagem:</label>
        <input id="messageText" type="text" [(ngModel)]="messageText" placeholder="Digite sua mensagem..." />
        <button (click)="sendMessage()" [disabled]="!messageText || !roomCode || !userName || !isGameServiceConnected()">
          Enviar
        </button>
      </div>
    </div>

    <style>
      .chat-container {
        font-family: Arial, sans-serif;
        max-width: 600px;
        margin: 20px auto;
        padding: 20px;
        border: 1px solid #ccc;
        border-radius: 8px;
        box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
      }
      .controls, .message-input {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        margin-bottom: 20px;
        align-items: center;
      }
      .controls label, .message-input label {
        flex-basis: 100%; /* Ocupa a linha toda em telas pequenas */
        font-weight: bold;
      }
      .controls input, .message-input input {
        flex-grow: 1; /* Permite que o input cresça para preencher o espaço */
        padding: 8px;
        border: 1px solid #ddd;
        border-radius: 4px;
        min-width: 150px; /* Garante que o input não fique muito pequeno */
      }
      .controls button, .message-input button {
        padding: 8px 15px;
        border: none;
        border-radius: 4px;
        background-color: #007bff;
        color: white;
        cursor: pointer;
        transition: background-color 0.2s ease;
      }
      .controls button:hover:not(:disabled), .message-input button:hover:not(:disabled) {
        background-color: #0056b3;
      }
      .controls button:disabled, .message-input button:disabled {
        background-color: #cccccc;
        cursor: not-allowed;
      }
      .messages-display {
        border: 1px solid #eee;
        padding: 10px;
        height: 300px;
        overflow-y: auto;
        background-color: #f9f9f9;
        border-radius: 4px;
        margin-bottom: 20px;
      }
      .messages-display p {
        margin: 5px 0;
        padding-bottom: 5px;
        border-bottom: 1px dotted #e0e0e0;
      }
      .messages-display p:last-child {
        border-bottom: none;
      }
      h2, h3 {
        color: #333;
        text-align: center;
        margin-bottom: 20px;
      }
    </style>
  `
})
export class ChatTesteComponent implements OnInit, OnDestroy {
  // Injeta o GameService usando a função 'inject()', um recurso moderno do Angular.
  public gameService: GameService = inject(GameService);

  // Signals para o estado dos inputs do formulário.
  // Embora para inputs simples ngModel e variáveis normais funcionem,
  // usar signals pode ser útil para cenários mais complexos de reatividade.
  public roomCode: string = '';
  public userName: string = '';
  public messageText: string = '';

  ngOnInit(): void {
    // Ao iniciar o componente, tenta conectar ao SignalR.
    // Em um aplicativo real, você pode querer controlar isso de forma mais granular.
    this.gameService.connect();
  }

  ngOnDestroy(): void {
    // Ao destruir o componente, desconecta do SignalR para liberar recursos.
    this.gameService.disconnect();
  }

  /**
   * Verifica se o GameService está conectado ao SignalR.
   * Usado para habilitar/desabilitar botões.
   */
  public isGameServiceConnected(): boolean {
    // Em um serviço real, você poderia ter um signal que expõe o estado da conexão.
    // Por simplicidade aqui, verificamos a presença da conexão.
    return !!this.gameService['hubConnection'] &&
           this.gameService['hubConnection'].state === (window as any).signalR.HubConnectionState.Connected;
  }

  /**
   * Chama o método 'entrarNaSala' do GameService.
   */
  public async joinRoom(): Promise<void> {
    if (this.roomCode && this.userName) {
      await this.gameService.entrarNaSala(this.roomCode, this.userName);
    } else {
      console.warn('Por favor, preencha o código da sala e seu nome.');
    }
  }

  /**
   * Chama o método 'enviarMensagemTeste' do GameService.
   */
  public async sendMessage(): Promise<void> {
    if (this.roomCode && this.userName && this.messageText) {
      await this.gameService.enviarMensagemTeste(this.roomCode, this.userName, this.messageText);
      this.messageText = ''; // Limpa o campo de mensagem após o envio.
    } else {
      console.warn('Por favor, preencha a mensagem, código da sala e seu nome.');
    }
  }

  /**
   * Desconecta do SignalR.
   */
  public async disconnect(): Promise<void> {
    await this.gameService.disconnect();
    this.roomCode = '';
    this.userName = '';
    this.messageText = '';
  }
}