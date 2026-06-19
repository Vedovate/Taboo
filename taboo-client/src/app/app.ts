import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ChatTesteComponent } from './components/chat-teste/chat-teste.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, ChatTesteComponent],
  template: `
    <main>
      <h1>Taboo Online Game</h1>
      <app-chat-teste></app-chat-teste>
    </main>
  `,
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('taboo-client');
}

