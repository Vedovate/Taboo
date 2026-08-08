import { Component, input, output } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  standalone: true,
  selector: 'app-error-message',
  imports: [TranslatePipe],
  templateUrl: './error-message.component.html',
  styleUrls: ['./error-message.component.scss'],
})
export class ErrorMessageComponent {
  readonly messageKey = input.required<string>();
  readonly timeLeft = input(0);
  readonly dismiss = output<void>();
}
