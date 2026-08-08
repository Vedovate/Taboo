import { Component, input } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';

@Component({
  standalone: true,
  selector: 'app-tooltip',
  imports: [TranslatePipe],
  template: `
    <span class="tooltip-wrap">
      <button type="button" class="tooltip-btn" aria-label="Ajuda">?</button>
      <span class="tooltip-text" role="tooltip">{{ tooltipKey() | translate }}</span>
    </span>
  `,
  styles: `
    .tooltip-wrap {
      position: relative;
      display: inline-flex;
      align-items: center;
    }

    .tooltip-btn {
      width: 1.2rem;
      height: 1.2rem;
      border-radius: 50%;
      border: 1px solid rgba(245, 245, 245, 0.45);
      background: transparent;
      color: rgba(245, 245, 245, 0.75);
      font-size: 0.85rem;
      line-height: 1;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: help;
      padding: 0;
    }

    .tooltip-text {
      position: absolute;
      bottom: calc(100% + 8px);
      left: 50%;
      transform: translateX(-50%);
      width: max-content;
      max-width: 280px;
      white-space: normal;
      font-size: 1rem;
      line-height: 1.4;
      font-weight: 500;
      padding: 0.5rem 0.75rem;
      border-radius: 0.6rem;
      background: #00baff;
      color: #121212;
      text-align: left;
      opacity: 0;
      visibility: hidden;
      pointer-events: none;
      transition: opacity 0.15s, visibility 0.15s;
      z-index: 30;
    }

    .tooltip-wrap:hover .tooltip-text,
    .tooltip-wrap:focus-within .tooltip-text {
      opacity: 1;
      visibility: visible;
    }
  `,
})
export class TooltipComponent {
  tooltipKey = input.required<string>();
}
