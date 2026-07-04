import { Component } from '@angular/core';
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
  currentLanguage = 'pt-BR';
  roomCode = '';
  showRoomCode = false;
  generatedRoomCode = '';
  isHost = false;
  hostName = '';

  constructor(private translateService: TranslateService, private router: Router, public gameService: GameService) {
    void this.translateService.use(this.currentLanguage);
  }

  get languageFlag(): string {
    return this.currentLanguage === 'pt-BR' ? '🇧🇷' : '🇺🇸';
  }

  toggleLanguage(): void {
    this.currentLanguage = this.currentLanguage === 'pt-BR' ? 'en-US' : 'pt-BR';
    void this.translateService.use(this.currentLanguage);
  }

  async goToLobby(): Promise<void> {
    const baseName = this.currentLanguage === 'pt-BR' ? 'Jogador 1' : 'Player 1';
    const maximumAttempts = 10;

    for (let attempt = 0; attempt < maximumAttempts; attempt += 1) {
      const roomCode = this.generateRoomCode();
      await this.gameService.createRoom(roomCode, baseName);

      if (this.gameService.connected()) {
        this.generatedRoomCode = roomCode;
        this.isHost = true;
        this.hostName = baseName;
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
    this.showRoomCode = !this.showRoomCode;
  }

  onRoomCodeInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.roomCode = target.value.toUpperCase();
  }

  async joinExistingRoom(): Promise<void> {
    const code = this.roomCode.trim().toUpperCase();
    if (!code) {
      this.gameService.error.set(this.translateService.instant('HOME.ERROR_ENTER_CODE'));
      return;
    }

    const baseName = this.currentLanguage === 'pt-BR' ? 'Jogador 2' : 'Player 2';
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
