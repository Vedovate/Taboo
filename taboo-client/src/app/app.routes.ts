import { Routes } from '@angular/router';
import { App } from './app';
import { ChatTesteComponent } from './components/chat-teste/chat-teste.component';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./app').then(m => m.App) },
  { 
    path: 'sala/:code', 
    loadComponent: () => import('./components/chat-teste/chat-teste.component').then(m => m.ChatTesteComponent),
  }
];
