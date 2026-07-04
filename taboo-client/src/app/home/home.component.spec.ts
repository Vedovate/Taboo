import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { By } from '@angular/platform-browser';
import { HomeComponent } from './home.component';
import { GameService } from '../services/game.service';
import { TranslateService } from '../services/translate.service';

interface MockGameService {
  error: WritableSignal<string>;
  connected: WritableSignal<boolean>;
  roomCode: WritableSignal<string>;
  players: WritableSignal<{ name: string; isHost: boolean }[]>;
  messages: WritableSignal<string[]>;
  createRoom: ReturnType<typeof vi.fn>;
  conectar: ReturnType<typeof vi.fn>;
  clearError: ReturnType<typeof vi.fn>;
  getRoomCode: ReturnType<typeof vi.fn>;
  getPlayers: ReturnType<typeof vi.fn>;
}

interface MockTranslateService {
  language: WritableSignal<string>;
  translations: WritableSignal<Record<string, string>>;
  loading: WritableSignal<boolean>;
  error: WritableSignal<string>;
  use: ReturnType<typeof vi.fn>;
  translate: ReturnType<typeof vi.fn>;
  instant: ReturnType<typeof vi.fn>;
}

describe('HomeComponent', () => {
  let component: HomeComponent;
  let fixture: ComponentFixture<HomeComponent>;
  let mockGameService: MockGameService;
  let mockTranslateService: MockTranslateService;

  beforeEach(async () => {
    mockGameService = {
      error: signal(''),
      connected: signal(false),
      roomCode: signal(''),
      players: signal([]),
      messages: signal([]),
      createRoom: vi.fn().mockResolvedValue(undefined),
      conectar: vi.fn().mockResolvedValue(undefined),
      clearError: vi.fn(),
      getRoomCode: vi.fn().mockReturnValue(''),
      getPlayers: vi.fn().mockReturnValue([]),
    };

    mockTranslateService = {
      language: signal('pt-BR'),
      translations: signal({}),
      loading: signal(false),
      error: signal(''),
      use: vi.fn().mockResolvedValue(undefined),
      translate: vi.fn((key: string) => key),
      instant: vi.fn((key: string) => key),
    };

    await TestBed.configureTestingModule({
      imports: [HomeComponent],
      providers: [
        provideRouter([]),
        { provide: GameService, useValue: mockGameService },
        { provide: TranslateService, useValue: mockTranslateService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HomeComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should call translateService.use on construction', () => {
    expect(mockTranslateService.use).toHaveBeenCalledWith('pt-BR');
  });

  describe('toggleLanguage', () => {
    it('should switch from pt-BR to en-US', () => {
      component.currentLanguage = 'pt-BR';

      component.toggleLanguage();

      expect(component.currentLanguage).toBe('en-US');
    });

    it('should switch from en-US to pt-BR', () => {
      component.currentLanguage = 'en-US';

      component.toggleLanguage();

      expect(component.currentLanguage).toBe('pt-BR');
    });

    it('should call translateService.use with new language', () => {
      component.toggleLanguage();

      expect(mockTranslateService.use).toHaveBeenCalledWith('en-US');
    });
  });

  describe('languageFlag', () => {
    it('should return Brazilian flag for pt-BR', () => {
      component.currentLanguage = 'pt-BR';
      expect(component.languageFlag).toBe('🇧🇷');
    });

    it('should return US flag for en-US', () => {
      component.currentLanguage = 'en-US';
      expect(component.languageFlag).toBe('🇺🇸');
    });
  });

  describe('goToLobby', () => {
    it('should call gameService.createRoom and navigate on success', async () => {
      mockGameService.connected.set(true);
      mockGameService.error.set('');
      const navigateSpy = vi.spyOn((component as any).router, 'navigate').mockResolvedValue(true);

      await component.goToLobby();

      expect(mockGameService.createRoom).toHaveBeenCalled();
      expect(navigateSpy).toHaveBeenCalledWith(['/lobby']);
      expect(component.isHost).toBe(true);
    });

    it('should not navigate if createRoom fails', async () => {
      mockGameService.connected.set(false);
      mockGameService.error.set('Error');
      const navigateSpy = vi.spyOn((component as any).router, 'navigate');

      await component.goToLobby();

      expect(navigateSpy).not.toHaveBeenCalled();
    });
  });

  describe('joinExistingRoom', () => {
    it('should call gameService.conectar and navigate on success', async () => {
      const navigateSpy = vi.spyOn((component as any).router, 'navigate').mockResolvedValue(true);
      mockGameService.connected.set(true);
      component.roomCode = 'ABC12';

      await component.joinExistingRoom();

      expect(mockGameService.conectar).toHaveBeenCalledWith('ABC12', 'Jogador 2');
      expect(navigateSpy).toHaveBeenCalledWith(['/lobby']);
    });

    it('should set error when room code is empty', async () => {
      component.roomCode = '';

      await component.joinExistingRoom();

      expect(mockGameService.error()).toBeTruthy();
      expect(mockGameService.conectar).not.toHaveBeenCalled();
    });
  });

  describe('onRoomCodeInput', () => {
    it('should convert input to uppercase', () => {
      const input = document.createElement('input');
      input.value = 'abcd12';
      const event = new Event('input', { bubbles: true });
      Object.defineProperty(event, 'target', { value: input, writable: false });

      component.onRoomCodeInput(event);

      expect(component.roomCode).toBe('ABCD12');
    });
  });

  describe('template', () => {
    it('should display error message when gameService.error is set', () => {
      mockGameService.error.set('Room not found');
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.error-message');
      expect(errorEl).toBeTruthy();
      expect(errorEl.textContent).toContain('Room not found');
    });

    it('should hide error message when there is no error', () => {
      mockGameService.error.set('');
      fixture.detectChanges();

      const errorEl = fixture.nativeElement.querySelector('.error-message');
      expect(errorEl).toBeFalsy();
    });

    it('should disable join button when roomCode is empty', () => {
      const joinBtn = fixture.debugElement.query(By.css('.btn-secondary')).nativeElement;
      expect(joinBtn.disabled).toBe(true);
    });

    it('should enable join button when roomCode has value', () => {
      const input = fixture.nativeElement.querySelector('#roomCode') as HTMLInputElement;
      input.value = 'abc';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const joinBtn = fixture.debugElement.query(By.css('.btn-secondary')).nativeElement;
      expect(joinBtn.disabled).toBe(false);
    });
  });
});
