import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { HostLobbyComponent } from './host-lobby.component';
import { GameService } from '../services/game.service';

interface MockGameService {
  error: WritableSignal<string>;
  connected: WritableSignal<boolean>;
  roomCode: WritableSignal<string>;
  players: WritableSignal<{ name: string; isHost: boolean }[]>;
  messages: WritableSignal<string[]>;
  createRoom: ReturnType<typeof vi.fn>;
  conectar: ReturnType<typeof vi.fn>;
  clearError: ReturnType<typeof vi.fn>;
}

describe('HostLobbyComponent', () => {
  let component: HostLobbyComponent;
  let fixture: ComponentFixture<HostLobbyComponent>;
  let mockGameService: MockGameService;

  beforeEach(async () => {
    mockGameService = {
      error: signal(''),
      connected: signal(true),
      roomCode: signal('ABC12'),
      players: signal([
        { name: 'Player1', isHost: true },
        { name: 'Player2', isHost: false },
      ]),
      messages: signal([]),
      createRoom: vi.fn().mockResolvedValue(undefined),
      conectar: vi.fn().mockResolvedValue(undefined),
      clearError: vi.fn(),
    };

    await TestBed.configureTestingModule({
      imports: [HostLobbyComponent],
      providers: [
        provideRouter([]),
        { provide: GameService, useValue: mockGameService },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HostLobbyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display room code from gameService', () => {
    const lobbyLabel = fixture.nativeElement.querySelector('.lobby-label');
    expect(lobbyLabel.textContent).toContain('ABC12');
  });

  it('should display player list from gameService', () => {
    const playerItems = fixture.nativeElement.querySelectorAll('.player-row');
    expect(playerItems.length).toBe(2);
    expect(playerItems[0].textContent).toContain('Player1');
    expect(playerItems[1].textContent).toContain('Player2');
  });

  it('should show HOST tag for host player', () => {
    const playerStatuses = fixture.nativeElement.querySelectorAll('.player-status');
    expect(playerStatuses[0].textContent).toContain('HOST');
  });

  it('should show JOGADOR tag for non-host player', () => {
    const playerStatuses = fixture.nativeElement.querySelectorAll('.player-status');
    expect(playerStatuses[1].textContent).toContain('JOGADOR');
  });

  describe('navigateBack', () => {
    it('should navigate to home', () => {
      const navigateSpy = vi.spyOn((component as any).router, 'navigate').mockResolvedValue(true);

      component.navigateBack();

      expect(navigateSpy).toHaveBeenCalledWith(['/']);
    });
  });

  describe('randomizeTeams', () => {
    it('should not throw', () => {
      expect(() => component.randomizeTeams()).not.toThrow();
    });
  });

  describe('template', () => {
    it('should have a timer slider with default value', () => {
      const timerEl = fixture.nativeElement.querySelector('.slider-row span');
      expect(timerEl.textContent).toContain('30');
      expect(component.timer).toBe(30);
    });

    it('should have a max score input with default value', () => {
      const scoreInput = fixture.nativeElement.querySelector('input[type="number"]');
      expect(scoreInput.value).toBe('1');
      expect(component.maxScore).toBe(1);
    });

    it('should have a VOLTAR button', () => {
      const backBtn = fixture.nativeElement.querySelector('.btn-secondary');
      expect(backBtn).toBeTruthy();
      expect(backBtn.textContent).toContain('VOLTAR');
    });
  });
});
