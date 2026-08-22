import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Component } from '@angular/core';
import { FitTextDirective } from './fit-text.directive';

@Component({
  standalone: true,
  imports: [FitTextDirective],
  template: `<div class="wrapper"><span appFitText>TESTE</span></div>`,
})
class HostComponent {}

class MockResizeObserver {
  observe(): void {}
  disconnect(): void {}
  unobserve(): void {}
}

function mockLayout(element: HTMLElement, width: number): void {
  Object.defineProperty(element, 'clientWidth', { configurable: true, value: width });
  Object.defineProperty(element, 'scrollWidth', { configurable: true, value: width });
  element.getBoundingClientRect = () => ({
    width,
    height: 30,
    top: 0,
    bottom: 30,
    left: 0,
    right: width,
    x: 0,
    y: 0,
    toJSON: () => {},
  });
}

describe('FitTextDirective', () => {
  let fixture: ComponentFixture<HostComponent>;
  let span: HTMLElement;
  let wrapper: HTMLElement;

  beforeEach(async () => {
    (globalThis as any).ResizeObserver = MockResizeObserver;

    await TestBed.configureTestingModule({
      imports: [HostComponent],
    }).compileComponents();
  });

  function createFixture(wrapperWidth: number, textScrollWidth: number): void {
    fixture = TestBed.createComponent(HostComponent);
    wrapper = fixture.nativeElement.querySelector('.wrapper');
    span = fixture.nativeElement.querySelector('span');
    mockLayout(wrapper, wrapperWidth);
    mockLayout(span, textScrollWidth);
    fixture.detectChanges();
  }

  it('should create with display block and clip style applied', () => {
    createFixture(200, 120);
    expect(span).toBeTruthy();
    expect(span.style.textOverflow).toBe('clip');
  });

  it('should keep the full font size when the text fits the container', () => {
    createFixture(200, 100);
    expect(parseInt(span.style.fontSize, 10)).toBe(40);
  });

  it('should shrink the font when the text is wider than the container', () => {
    createFixture(200, 400);
    const fontSize = parseInt(span.style.fontSize, 10);
    expect(fontSize).toBeGreaterThanOrEqual(16);
    expect(fontSize).toBeLessThanOrEqual(40);
  });

  it('should fit any long text within the container width', () => {
    createFixture(200, 900);
    const fontSize = parseInt(span.style.fontSize, 10);
    expect(fontSize).toBeGreaterThanOrEqual(16);
    expect(fontSize).toBeLessThanOrEqual(40);
  });

  it('should account for the parent padding when fitting the text', () => {
    fixture = TestBed.createComponent(HostComponent);
    wrapper = fixture.nativeElement.querySelector('.wrapper');
    span = fixture.nativeElement.querySelector('span');
    wrapper.style.paddingLeft = '40px';
    wrapper.style.paddingRight = '40px';
    mockLayout(wrapper, 200);
    mockLayout(span, 150);
    fixture.detectChanges();
    const fontSize = parseInt(span.style.fontSize, 10);
    expect(fontSize).toBeGreaterThanOrEqual(16);
    expect(fontSize).toBeLessThanOrEqual(40);
  });

  it('should keep the full font size when the text fits the content width (ignoring padding)', () => {
    fixture = TestBed.createComponent(HostComponent);
    wrapper = fixture.nativeElement.querySelector('.wrapper');
    span = fixture.nativeElement.querySelector('span');
    wrapper.style.paddingLeft = '40px';
    wrapper.style.paddingRight = '40px';
    mockLayout(wrapper, 200);
    mockLayout(span, 100);
    fixture.detectChanges();
    expect(parseInt(span.style.fontSize, 10)).toBe(40);
  });
});
