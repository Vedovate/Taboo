import { Component, inject } from '@angular/core';
import { signal } from '@angular/core';
import { GameService } from '../../services/game.service';

@Component({
  selector: 'app-chat-teste',
  standalone: true,
  templateUrl: './chat-teste.component.html',
  styles: [`
    :host {
      display: block;
      min-height: calc(100vh - 48px);
      padding: 24px;
    }

    .sala-container {
      max-width: 960px;
      margin: 0 auto;
      border-radius: 16px;
      background-color: var(--bg-primary);
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.4);
    }

    .page-header {
      text-align: center;
      padding-bottom: 24px;
      border-bottom: 1px solid rgba(59, 130, 246, 0.2);
      margin-bottom: 32px;
    }

    .page-header h1 {
      margin: 0 0 8px;
      font-size: 1.75rem;
      color: var(--white);
    }

    .subtitle {
      margin: 0;
      color: rgba(245, 247, 250, 0.6);
      font-size: 1rem;
    }

    .grid {
      display: grid;
      gap: 16px;
      grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
    }

    @media (min-width: 768px) {
      .grid {
        grid-template-columns: 2fr 1fr;
      }
    }

    .panel {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 20px;
      border-radius: 16px;
      background-color: rgba(59, 130, 246, 0.08);
      border: 1px solid rgba(59, 130, 246, 0.15);
    }

    .panel-title {
      margin: 0;
      font-size: 1rem;
      color: var(--white);
      font-weight: 600;
    }

    .row {
      display: flex;
      flex-direction: column;
      gap: 8px;
    }

    .form-label {
      font-size: 0.9rem;
      color: var(--white);
      font-weight: 500;
    }

    input[type="text"] {
      width: 100%;
      box-sizing: border-box;
      padding: 14px 16px;
      border-radius: 12px;
      border: 2px solid rgba(59, 130, 246, 0.3);
      background-color: rgba(15, 23, 42, 0.8);
      color: var(--white);
      font-size: 1rem;
      outline: none;
      transition: border-color 0.2s ease, box-shadow 0.2s ease;
    }

    input[type="text"]::placeholder {
      color: rgba(245, 247, 250, 0.3);
    }

    input[type="text"]:focus {
      border-color: var(--blue-500);
      box-shadow: 0 0 0 4px rgba(59, 130, 246, 0.15);
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    button {
      border: none;
      border-radius: 999px;
      padding: 14px 28px;
      font-size: 0.95rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
    }

    .btn-primary {
      background: linear-gradient(135deg, var(--blue-500), #60a5fa);
      color: white;
      box-shadow: 0 4px 14px rgba(59, 130, 246, 0.4);
    }

    .btn-primary:hover:not(:disabled) {
      background: linear-gradient(135deg, #2563eb, var(--blue-500));
      transform: translateY(-1px);
      box-shadow: 0 6px 20px rgba(59, 130, 246, 0.5);
    }

    .btn-primary:disabled {
      opacity: 0.5;
      cursor: not-allowed;
      transform: none;
      box-shadow: none;
    }

    .full-width {
      width: 100%;
    }

    .btn-danger {
      background: linear-gradient(135deg, var(--red-500), #f87171);
      color: white;
      box-shadow: 0 4px 14px rgba(239, 68, 68, 0.4);
    }

    .btn-danger:hover:not(:disabled) {
      background: linear-gradient(135deg, #dc2626, var(--red-500));
      transform: translateY(-1px);
      box-shadow: 0 6px 20px rgba(239, 68, 68, 0.5);
    }

    .status-indicator {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
      background-color: rgba(34, 197, 94, 0.15);
      border-radius: 999px;
      color: #22c55e;
      font-weight: 500;
    }

    .status-indicator::before {
      content: '';
      width: 8px;
      height: 8px;
      background-color: #22c55e;
      border-radius: 50%;
      animation: pulse 2s ease-in-out infinite;
    }

    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }

    .error-text, .info-text, .empty {
      margin: 0;
      font-size: 0.87rem;
      line-height: 1.6;
    }

    .disabled-btn {
      cursor: default !important;
    }

    .error-message {
      padding: 12px 14px;
      background-color: rgba(239, 68, 68, 0.15);
      border-radius: 12px;
      color: var(--red-500);
      text-align: center;
    }

    .error-message::before {
      content: '⚠️';
      margin-right: 6px;
    }

    .info-text {
      padding: 14px;
      background-color: rgba(34, 211, 238, 0.1);
      border-radius: 12px;
      color: rgba(96, 165, 250, 0.7);
    }

    .messages {
      min-height: 400px;
      max-height: 500px;
      overflow-y: auto;
      padding: 20px;
      border-radius: 16px;
      background-color: rgba(34, 211, 238, 0.08);
      border: 1px solid rgba(59, 130, 246, 0.15);
    }

    .message-list {
      display: grid;
      gap: 12px;
      margin: 0;
      padding: 0;
      list-style: none;
    }

    .message-item {
      padding: 14px 16px;
      border-radius: 12px;
      background-color: rgba(59, 130, 246, 0.1);
      border-left: 3px solid var(--blue-500);
    }

    .empty {
      color: rgba(245, 247, 250, 0.4);
    }
  `]
})
export class ChatTesteComponent {
  readonly gameService = inject(GameService);

  readonly roomCode = signal('');
  
  readonly userName = signal('');
  readonly messageText = signal('');

  onRoomInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.roomCode.set(target.value);
  }

  onUserNameInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.userName.set(target.value);
  }

  onMessageInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.messageText.set(target.value);
  }

  async conectar(): Promise<void> {
    await this.gameService.conectar(this.roomCode(), this.userName());
    
    // Redirecionamento automático após conexão bem-sucedida
    if (this.gameService.connected()) {
      const code = this.roomCode();
      
      // Pequeno delay para garantir que o serviço já atualizou seu estado
      await new Promise(resolve => setTimeout(resolve, 100));
      
      window.location.href = `/sala/${code}`;
    }
  }

  async enviarMensagem(): Promise<void> {
    const texto = this.messageText().trim();

    if (!texto) {
      return;
    }

    await this.gameService.enviarMensagem(texto);
    this.messageText.set('');
  }

  async desconectar(): Promise<void> {
    await this.gameService.desconectar();
    // Ao desconectar, o usuário volta para a tela inicial (rota raiz)
    window.location.href = '/';
  }
}
