import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';

@Component({
  standalone: true,
  selector: 'app-root',
  imports: [RouterModule],
  template: `<router-outlet></router-outlet>`,
  styles: [`:host { display: block; min-height: 100vh; background-color: var(--bg-dark); color: var(--text); }`]
})
export class App {}
