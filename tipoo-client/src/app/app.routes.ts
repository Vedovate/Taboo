import { Routes } from '@angular/router';
import { gameGuard } from './game/game.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home/home.component').then((c) => c.HomeComponent),
  },
  {
    path: 'lobby',
    loadComponent: () => import('./home/host-lobby.component').then((c) => c.HostLobbyComponent),
  },
  {
    path: 'jogo',
    canActivate: [gameGuard],
    loadComponent: () => import('./game/clue-giver-screen.component').then((c) => c.ClueGiverScreenComponent),
  },
  { path: '**', redirectTo: '' },
];
