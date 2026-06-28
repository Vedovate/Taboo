import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadChildren: () => import('./home/home.module').then((m) => m.HomeModule),
  },
  {
    path: 'lobby',
    loadComponent: () => import('./home/host-lobby.component').then((c) => c.HostLobbyComponent),
  },
];
