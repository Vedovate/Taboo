import { Component, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideArrowRight, LucideHash, LucidePlus } from '@lucide/angular';
import { GameService } from '../services/game.service';
import { TranslatePipe } from '../pipes/translate.pipe';
import { TranslateService } from '../services/translate.service';
import { LogoPlaceholderComponent } from './logo-placeholder/logo-placeholder.component';

@Component({
  standalone: true,
  selector: 'app-home',
  imports: [TranslatePipe, LucidePlus, LucideHash, LucideArrowRight, LogoPlaceholderComponent],
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

  readonly languageFlag = computed(() =>
    this.currentLanguage() === 'pt-BR' ? '🇧🇷' : '🇺🇸'
  );

  constructor(private translateService: TranslateService, private router: Router, public gameService: GameService) {
    void this.translateService.use(this.currentLanguage());
  }

  toggleLanguage(): void {
    const next = this.currentLanguage() === 'pt-BR' ? 'en-US' : 'pt-BR';
    this.currentLanguage.set(next);
    void this.translateService.use(next);
  }

  async goToLobby(): Promise<void> {
    const baseName = this.currentLanguage() === 'pt-BR' ? 'Jogador 1' : 'Player 1';
    const maximumAttempts = 10;

    for (let attempt = 0; attempt < maximumAttempts; attempt += 1) {
      const code = this.generateRoomCode();
      await this.gameService.createRoom(code, baseName);

      if (this.gameService.connected()) {
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

    this.gameService.error.set(this.translateService.instant('HOME.ERROR_CREATE_ROOM_UNIQUE'));
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
      this.gameService.error.set(this.translateService.instant('HOME.ERROR_ENTER_CODE'));
      return;
    }

    const baseName = this.currentLanguage() === 'pt-BR' ? 'Jogador 2' : 'Player 2';
    await this.gameService.conectar(code, baseName);

    if (this.gameService.connected()) {
      this.router.navigate(['/lobby']);
    }
  }

  private generateRoomCode(): string {
    const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    return Array.from({ length: 5 }, () => chars.charAt(Math.floor(Math.random() * chars.length))).join('');
  }
}
