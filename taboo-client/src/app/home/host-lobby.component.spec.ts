import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { HostLobbyComponent } from './host-lobby.component';
import { GameService } from '../services/game.service';

interface MockGameService {
  error: WritableSignal<string>;
  connected: WritableSignal<boolean>;
  roomCode: WritableSignal<string>;
  players: WritableSignal<{ connectionId: string; name: string; isHost: boolean }[]>;
  messages: WritableSignal<string[]>;
  meuConnectionId: WritableSignal<string>;
  nomeFinalizado: WritableSignal<boolean>;
  createRoom: ReturnType<typeof vi.fn>;
  conectar: ReturnType<typeof vi.fn>;
  alterarNome: ReturnType<typeof vi.fn>;
  expulsarJogador: ReturnType<typeof vi.fn>;
  clearError: ReturnType<typeof vi.fn>;
}

describe('HostLobbyComponent', () => {
  let component: HostLobbyComponent;
  let fixture: ComponentFixture<HostLobbyComponent>;
  let mockGameService: MockGameService;
  const meuConnectionId = signal('');

  beforeEach(async () => {
    meuConnectionId.set('conn1');
    mockGameService = {
      error: signal(''),
      connected: signal(true),
      roomCode: signal('ABC12'),
      players: signal([
        { connectionId: 'conn1', name: 'Player1', isHost: true },
        { connectionId: 'conn2', name: 'Player2', isHost: false },
      ]),
      messages: signal([]),
      meuConnectionId,
      nomeFinalizado: signal(true),
      createRoom: vi.fn().mockResolvedValue(undefined),
      conectar: vi.fn().mockResolvedValue(undefined),
      alterarNome: vi.fn().mockResolvedValue(undefined),
      expulsarJogador: vi.fn().mockResolvedValue(undefined),
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

  it('should not show status tag for non-host player', () => {
    const playerStatuses = fixture.nativeElement.querySelectorAll('.player-status');
    expect(playerStatuses.length).toBe(1);
  });

  describe('canEdit', () => {
    it('should return true when connectionId matches and nomeFinalizado is true', () => {
      expect(component.canEdit({ connectionId: 'conn1', name: 'Player1', isHost: true })).toBe(true);
    });

    it('should return false when connectionId does not match', () => {
      expect(component.canEdit({ connectionId: 'conn2', name: 'Player2', isHost: false })).toBe(false);
    });

    it('should return false when nomeFinalizado is false', () => {
      mockGameService.nomeFinalizado.set(false);

      expect(component.canEdit({ connectionId: 'conn1', name: 'Player1', isHost: true })).toBe(false);
    });
  });

  describe('template', () => {
    it('should show edit button for own player when canEdit is true', () => {
      const iconBtns = fixture.nativeElement.querySelectorAll('.icon-btn');
      const editBtn = Array.from(iconBtns).find(
        (btn: any) => btn.getAttribute('aria-label') === 'Editar Nome'
      );
      expect(editBtn).toBeTruthy();
    });

    it('should show kick button only for host on non-host players', () => {
      const kickButtons = fixture.nativeElement.querySelectorAll('.icon-btn-danger');
      expect(kickButtons.length).toBe(1);
    });

    it('should call expulsarJogador when kick button is clicked', () => {
      const kickButton = fixture.nativeElement.querySelector('.icon-btn-danger');
      kickButton.click();

      expect(mockGameService.expulsarJogador).toHaveBeenCalledWith('conn2');
    });

    it('should not show kick button when current player is not host', () => {
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      const kickButtons = fixture.nativeElement.querySelectorAll('.icon-btn-danger');
      expect(kickButtons.length).toBe(0);
    });

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
