import { Component } from '@angular/core';
import { Router } from '@angular/router';
import { GameService } from '../services/game.service';

@Component({
  standalone: true,
  selector: 'app-host-lobby',
  imports: [],
  templateUrl: './host-lobby.component.html',
  styleUrls: ['./host-lobby.component.scss'],
})
export class HostLobbyComponent {
  timer = 30;
  maxScore = 1;

  constructor(private router: Router, public gameService: GameService) {}

  navigateBack(): void {
    this.router.navigate(['/']);
  }

  randomizeTeams(): void {
    // placeholder for future randomization logic
  }
}
