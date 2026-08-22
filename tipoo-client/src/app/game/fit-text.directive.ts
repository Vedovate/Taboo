import { Directive, ElementRef, Input, NgZone, OnDestroy, OnInit } from '@angular/core';

@Directive({
  standalone: true,
  selector: '[appFitText]',
})
export class FitTextDirective implements OnInit, OnDestroy {
  @Input() minFontSize = 16;
  @Input() maxFontSize = 40;

  private observer: ResizeObserver | null = null;
  private mutationObserver: MutationObserver | null = null;

  constructor(
    private el: ElementRef<HTMLElement>,
    private ngZone: NgZone,
  ) {}

  ngOnInit(): void {
    const host = this.el.nativeElement;
    host.style.whiteSpace = 'nowrap';
    host.style.overflow = 'visible';
    host.style.textOverflow = 'clip';
    host.style.display = 'block';
    host.style.textAlign = 'center';
    host.style.width = '100%';

    this.ngZone.runOutsideAngular(() => {
      this.fit();
      this.observer = new ResizeObserver(() => this.fit());
      this.observer.observe(host);
      if (host.parentElement) {
        this.observer.observe(host.parentElement);
      }
      this.mutationObserver = new MutationObserver(() => this.fit());
      this.mutationObserver.observe(host, { childList: true, characterData: true, subtree: true });
    });
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
    this.observer = null;
    this.mutationObserver?.disconnect();
    this.mutationObserver = null;
  }

  private fit(): void {
    const host = this.el.nativeElement;
    const parent = host.parentElement;
    if (!parent) {
      return;
    }

    const parentStyle = getComputedStyle(parent);
    const paddingX =
      parseFloat(parentStyle.paddingLeft || '0') + parseFloat(parentStyle.paddingRight || '0');
    const rectWidth = parent.getBoundingClientRect ? parent.getBoundingClientRect().width : 0;
    const baseWidth = rectWidth > 0 ? rectWidth : parent.clientWidth;
    const parentWidth = baseWidth - paddingX;
    if (parentWidth <= 0) {
      return;
    }

    // Margem de segurança de 4% para garantir folga confortável
    const targetWidth = Math.floor(parentWidth * 0.96);

    // Configura temporariamente para medir a largura intrínseca real do texto
    host.style.display = 'inline-block';
    host.style.width = 'auto';
    host.style.maxWidth = 'none';
    host.style.whiteSpace = 'nowrap';
    host.style.textOverflow = 'clip';

    let low = this.minFontSize;
    let high = this.maxFontSize;

    while (low < high) {
      const mid = Math.ceil((low + high) / 2);
      host.style.fontSize = `${mid}px`;
      const textWidth = host.getBoundingClientRect ? host.getBoundingClientRect().width : host.scrollWidth;
      if (textWidth > targetWidth) {
        high = mid - 1;
      } else {
        low = mid;
      }
    }

    // Aplica o tamanho final encontrado
    host.style.fontSize = `${low}px`;
    host.style.lineHeight = '1.15';
    host.style.display = 'block';
    host.style.width = '100%';
    host.style.maxWidth = '100%';
    host.style.textAlign = 'center';
    host.style.overflow = 'visible';
    host.style.textOverflow = 'clip';

    // Se mesmo no tamanho mínimo a palavra ainda for maior que o container, quebra de linha segura
    const finalWidth = host.getBoundingClientRect ? host.getBoundingClientRect().width : host.scrollWidth;
    if (finalWidth > parentWidth) {
      host.style.whiteSpace = 'normal';
      host.style.wordBreak = 'break-word';
      host.style.overflowWrap = 'break-word';
    } else {
      host.style.whiteSpace = 'nowrap';
    }
  }
}
