import { Directive, ElementRef, NgZone, OnDestroy, OnInit } from '@angular/core';

@Directive({
  standalone: true,
  selector: '[appFitText]',
})
export class FitTextDirective implements OnInit, OnDestroy {
  private readonly minFontSize = 16;
  private readonly maxFontSize = 500;
  private observer: ResizeObserver | null = null;
  private mutationObserver: MutationObserver | null = null;

  constructor(
    private el: ElementRef<HTMLElement>,
    private ngZone: NgZone,
  ) {}

  ngOnInit(): void {
    const host = this.el.nativeElement;
    host.style.whiteSpace = 'nowrap';
    host.style.overflow = 'hidden';
    host.style.maxWidth = '100%';
    host.style.display = 'inline-block';

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

    let low = this.minFontSize;
    let high = this.maxFontSize;
    while (low < high) {
      const mid = Math.ceil((low + high) / 2);
      host.style.fontSize = `${mid}px`;
      if (host.scrollWidth > parentWidth) {
        high = mid - 1;
      } else {
        low = mid;
      }
    }
    host.style.fontSize = `${low}px`;
  }
}
