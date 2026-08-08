import { Component, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideArrowRight, LucideHash, LucidePlus } from '@lucide/angular';
import { GameService } from '../services/game.service';
import { HostSessionService } from '../services/host-session.service';
import { PlayerStorageService } from '../services/player-storage.service';
import { TranslatePipe } from '../pipes/translate.pipe';
import { TranslateService } from '../services/translate.service';
import { LogoComponent } from './logo/logo.component';
import { ErrorMessageComponent } from '../shared/error-message/error-message.component';

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [TranslatePipe, LucidePlus, LucideHash, LucideArrowRight, LogoComponent, ErrorMessageComponent],
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.scss'],
})
export class HomeComponent {
  currentLanguage = signal('pt-BR');
  roomCode = signal('');
  showRoomCode = signal(false);
  generatedRoomCode = signal('');
  isHost = signal(false);
  hostName = signal('');

  readonly languageFlag = computed(() => {
    const svg = this.currentLanguage() === 'pt-BR'
      ? '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 25"><rect width="36" height="25" fill="#009739"/><polygon points="18,3 30,12.5 18,22 6,12.5" fill="#FEDF00"/><circle cx="18" cy="12.5" r="5" fill="#002776"/></svg>'
      : '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 36 25"><rect width="36" height="25" fill="#B22234"/><rect y="1.92" width="36" height="1.92" fill="white"/><rect y="5.77" width="36" height="1.92" fill="white"/><rect y="9.62" width="36" height="1.92" fill="white"/><rect y="13.46" width="36" height="1.92" fill="white"/><rect y="17.31" width="36" height="1.92" fill="white"/><rect y="21.15" width="36" height="1.92" fill="white"/><rect width="15" height="13.46" fill="#3C3B6E"/></svg>';
    return `data:image/svg+xml,${encodeURIComponent(svg)}`;
  });

  constructor(
    private translateService: TranslateService,
    private router: Router,
    public gameService: GameService,
    private playerStorage: PlayerStorageService,
    private hostSession: HostSessionService,
  ) {
    void this.translateService.use(this.currentLanguage());
  }

  toggleLanguage(): void {
    const next = this.currentLanguage() === 'pt-BR' ? 'en-US' : 'pt-BR';
    this.currentLanguage.set(next);
    void this.translateService.use(next);
  }

  async goToLobby(): Promise<void> {
    const cached = this.playerStorage.loadName();
    const baseName = cached ?? (this.currentLanguage() === 'pt-BR' ? 'Jogador 1' : 'Player 1');
    const maximumAttempts = 10;

    for (let attempt = 0; attempt < maximumAttempts; attempt += 1) {
      const code = this.generateRoomCode();
      const sessionId = this.hostSession.getOrCreate();
      await this.gameService.createRoom(code, baseName, sessionId);

      if (this.gameService.connected()) {
        this.playerStorage.saveName(baseName);
        this.generatedRoomCode.set(code);
        this.isHost.set(true);
        this.hostName.set(baseName);
        this.router.navigate(['/lobby']);
        return;
      }

      if (attempt === 0 && this.gameService.error()) {
        return;
      }
    }

    this.gameService.setError(this.translateService.instant('HOME.ERROR_CREATE_ROOM_UNIQUE'));
  }

  toggleRoomCodeVisibility(): void {
    this.showRoomCode.update(v => !v);
  }

  onRoomCodeInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.roomCode.set(target.value.toUpperCase());
  }

  async joinExistingRoom(): Promise<void> {
    const code = this.roomCode().trim().toUpperCase();
    if (!code) {
      this.gameService.setError(this.translateService.instant('HOME.ERROR_ENTER_CODE'));
      return;
    }

    const cached = this.playerStorage.loadName();
    const baseName = cached ?? (this.currentLanguage() === 'pt-BR' ? `Jogador ${this.gameService.playerCount() + 1}` : `Player ${this.gameService.playerCount() + 1}`);
    await this.gameService.conectar(code, baseName);

    if (this.gameService.connected()) {
      this.playerStorage.saveName(baseName);
      this.router.navigate(['/lobby']);
    }
  }

  private generateRoomCode(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    return Array.from({ length: 5 }, () => chars.charAt(Math.floor(Math.random() * chars.length))).join('');
  }
}
