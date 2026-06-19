import { Component, signal } from '@angular/core';
import { inject } from '@angular/core';
import { GameService } from './services/game.service';

@Component({
  selector: 'app-root',
  standalone: true,
  template: `
    <main class="lobby">
      <header class="page-header">
        <h1>Sala de Chat</h1>
        <p class="subtitle">Conecte-se a uma sala e participe do chat em tempo real</p>
      </header>

      <section class="connection-form">
        <div class="input-group">
          <label for="roomCode" class="form-label">Código da Sala</label>
          <input 
            id="roomCode" 
            type="text" 
            [value]="roomCode()" 
            (input)="onRoomInput($event)" 
            placeholder="Ex: SALA123"
            autocomplete="off"
          />
        </div>

        <div class="input-group">
          <label for="userName" class="form-label">Nome do Usuário</label>
          <input 
            id="userName" 
            type="text" 
            [value]="userName()" 
            (input)="onUserNameInput($event)" 
            placeholder="Ex: Leonardo"
            autocomplete="off"
          />
        </div>

        <div class="actions">
          <button 
            type="button" 
            (click)="conectar()" 
            [disabled]="!roomCode().trim() || !userName().trim() || gameService.connected()"
            class="btn btn-primary"
          >
            Conectar à Sala
          </button>
          
          @if (gameService.connected()) {
            <span class="status-indicator">
              ● Conectado na sala {{ roomCode() }}
            </span>
          }
        </div>

        @if (!gameService.connected()) {
          <p class="info-text">
            Digite o código da sala e seu nome, depois clique em "Conectar à Sala". 
            Ao conectar com sucesso, você será redirecionado automaticamente para a sala de chat.
          </p>
        }

        @if (gameService.error()) {
          <div class="error-message">
            {{ gameService.error() }}
          </div>
        }
      </section>
    </main>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100vh;
      padding: 24px;
    }

    .lobby {
      max-width: 680px;
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
      font-size: 2rem;
      color: var(--white);
      background: linear-gradient(135deg, var(--blue-500), #a78bfa);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
    }

    .subtitle {
      margin: 0;
      color: rgba(245, 247, 250, 0.6);
      font-size: 1rem;
    }

    .connection-form {
      display: grid;
      gap: 20px;
    }

    .input-group {
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
      gap: 16px;
      margin-top: 8px;
    }

    .btn {
      border: none;
      border-radius: 999px;
      padding: 14px 28px;
      font-size: 1rem;
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

    .status-indicator {
      display: inline-flex;
      align-items: center;
      gap: 8px;
      padding: 12px 16px;
      background-color: rgba(59, 130, 246, 0.2);
      border-radius: 999px;
      color: var(--blue-500);
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

    .info-text {
      margin-top: 8px;
      padding: 16px;
      background-color: rgba(34, 211, 238, 0.1);
      border-radius: 12px;
      color: rgba(96, 165, 250, 0.7);
      font-size: 0.87rem;
      line-height: 1.6;
    }

    .error-message {
      margin-top: 16px;
      padding: 14px 16px;
      background-color: rgba(239, 68, 68, 0.15);
      border-radius: 12px;
      color: var(--red-500);
      font-size: 0.9rem;
      text-align: center;
    }

    .error-message::before {
      content: '⚠️';
      margin-right: 8px;
    }
  `]
})
export class App {
  readonly roomCode = signal('');
  
  readonly userName = signal('');

  onRoomInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.roomCode.set(target.value);
  }

  onUserNameInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.userName.set(target.value);
  }

  readonly gameService = inject(GameService);
  
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
}
