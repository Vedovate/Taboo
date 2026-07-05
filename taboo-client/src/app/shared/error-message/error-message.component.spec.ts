import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ErrorMessageComponent } from './error-message.component';
import { TranslateService } from '../../services/translate.service';
import { signal } from '@angular/core';

describe('ErrorMessageComponent', () => {
  let component: ErrorMessageComponent;
  let fixture: ComponentFixture<ErrorMessageComponent>;
  const mockTranslateService = {
    translations: signal({}),
    translate: vi.fn((key: string) => key),
    instant: vi.fn((key: string) => key),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ErrorMessageComponent],
      providers: [
        { provide: TranslateService, useValue: mockTranslateService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorMessageComponent);
    component = fixture.componentInstance;
    fixture.componentRef.setInput('messageKey', 'Test error');
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the error message in bold', () => {
    const errorEl = fixture.nativeElement.querySelector('.error-text');
    expect(errorEl).toBeTruthy();
    expect(errorEl.textContent).toContain('Test error');
    expect(errorEl.tagName).toBe('STRONG');
  });

  it('should show countdown when timeLeft > 0', () => {
    fixture.componentRef.setInput('timeLeft', 5);
    fixture.detectChanges();

    const countdown = fixture.nativeElement.querySelector('.error-countdown');
    expect(countdown).toBeTruthy();
    expect(countdown.textContent).toContain('(5s)');
  });

  it('should hide countdown when timeLeft is 0', () => {
    fixture.componentRef.setInput('timeLeft', 0);
    fixture.detectChanges();

    const countdown = fixture.nativeElement.querySelector('.error-countdown');
    expect(countdown).toBeFalsy();
  });

  it('should emit dismiss when close button is clicked', () => {
    const dismissSpy = vi.fn();
    component.dismiss.subscribe(dismissSpy);

    const closeBtn = fixture.nativeElement.querySelector('.error-close');
    closeBtn.click();

    expect(dismissSpy).toHaveBeenCalledTimes(1);
  });

  it('should have a close button', () => {
    const closeBtn = fixture.nativeElement.querySelector('.error-close');
    expect(closeBtn).toBeTruthy();
    expect(closeBtn.textContent).toContain('✕');
  });
});
