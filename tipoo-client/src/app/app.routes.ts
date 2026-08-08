import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./home/home.component').then((c) => c.HomeComponent),
  },
  {
    path: 'lobby',
    loadComponent: () => import('./home/host-lobby.component').then((c) => c.HostLobbyComponent),
  },
  { path: '**', redirectTo: '' },
];
