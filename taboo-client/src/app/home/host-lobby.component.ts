import { Component, computed } from '@angular/core';
import { Router } from '@angular/router';
import { LucideUserX } from '@lucide/angular';
import { GameService } from '../services/game.service';
import { LobbyPlayer } from '../models/lobby-player';

@Component({
  standalone: true,
  selector: 'app-host-lobby',
  imports: [LucideUserX],
  templateUrl: './host-lobby.component.html',
  styleUrls: ['./host-lobby.component.scss'],
})
export class HostLobbyComponent {
  timer = 30;
  maxScore = 1;

  readonly souHost = computed(() =>
    this.gameService.players().some(p => p.connectionId === this.gameService.meuConnectionId() && p.isHost)
  );

  constructor(private router: Router, public gameService: GameService) {}

  canEdit(player: LobbyPlayer): boolean {
    return player.connectionId === this.gameService.meuConnectionId()
      && this.gameService.nomeFinalizado();
  }

  navigateBack(): void {
    this.router.navigate(['/']);
  }

  expulsarJogador(connectionId: string): void {
    this.gameService.expulsarJogador(connectionId);
  }

  randomizeTeams(): void {
    // placeholder for future randomization logic
  }
}
