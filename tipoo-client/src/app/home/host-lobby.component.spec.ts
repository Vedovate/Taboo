import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal, WritableSignal } from '@angular/core';
import { HostLobbyComponent } from './host-lobby.component';
import { GameService } from '../services/game.service';
import { PlayerStorageService } from '../services/player-storage.service';
import { CardOptions } from '../models/card-options';
import { createDefaultGameSettings, GameSettings } from '../models/game-settings';

interface MockGameService {
  error: WritableSignal<string>;
  connected: WritableSignal<boolean>;
  roomCode: WritableSignal<string>;
  players: WritableSignal<{ connectionId: string; name: string; isHost: boolean; team: string; isReady: boolean }[]>;
  playerCount: WritableSignal<number>;
  messages: WritableSignal<string[]>;
  meuConnectionId: WritableSignal<string>;
  nomeFinalizado: WritableSignal<boolean>;
  settings: WritableSignal<GameSettings>;
  cardOptions: WritableSignal<CardOptions>;
  createRoom: ReturnType<typeof vi.fn>;
  conectar: ReturnType<typeof vi.fn>;
  alterarNome: ReturnType<typeof vi.fn>;
  expulsarJogador: ReturnType<typeof vi.fn>;
  sairDaSala: ReturnType<typeof vi.fn>;
  desconectar: ReturnType<typeof vi.fn>;
  clearError: ReturnType<typeof vi.fn>;
  escolherTime: ReturnType<typeof vi.fn>;
  alternarPronto: ReturnType<typeof vi.fn>;
  randomizarTime: ReturnType<typeof vi.fn>;
  forcarIniciar: ReturnType<typeof vi.fn>;
  configurarPartida: ReturnType<typeof vi.fn>;
}

describe('HostLobbyComponent', () => {
  let component: HostLobbyComponent;
  let fixture: ComponentFixture<HostLobbyComponent>;
  let mockGameService: MockGameService;
  let mockPlayerStorage: { saveName: ReturnType<typeof vi.fn>; loadName: ReturnType<typeof vi.fn> };
  const meuConnectionId = signal('');

  beforeEach(async () => {
    meuConnectionId.set('conn1');
    mockGameService = {
      error: signal(''),
      connected: signal(true),
      roomCode: signal('ABC12'),
      players: signal([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: '', isReady: false },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false },
      ]),
      messages: signal([]),
      playerCount: signal(2),
      meuConnectionId,
      nomeFinalizado: signal(true),
      settings: signal(createDefaultGameSettings()),
      cardOptions: signal({ dificuldades: ['Fácil', 'Médio', 'Difícil'], categorias: ['Objeto', 'Tecnologia', 'Conceito'] }),
      createRoom: vi.fn().mockResolvedValue(undefined),
      conectar: vi.fn().mockResolvedValue(undefined),
      alterarNome: vi.fn().mockResolvedValue(true),
      expulsarJogador: vi.fn().mockResolvedValue(undefined),
      sairDaSala: vi.fn().mockResolvedValue(true),
      desconectar: vi.fn().mockResolvedValue(undefined),
      clearError: vi.fn(),
      escolherTime: vi.fn().mockResolvedValue(true),
      alternarPronto: vi.fn().mockResolvedValue(true),
      randomizarTime: vi.fn().mockResolvedValue('Vermelho'),
      forcarIniciar: vi.fn().mockResolvedValue(true),
      configurarPartida: vi.fn().mockResolvedValue(createDefaultGameSettings()),
    };

    mockPlayerStorage = {
      saveName: vi.fn(),
      loadName: vi.fn(() => null),
    };

    await TestBed.configureTestingModule({
      imports: [HostLobbyComponent],
      providers: [
        provideRouter([]),
        { provide: GameService, useValue: mockGameService },
        { provide: PlayerStorageService, useValue: mockPlayerStorage },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(HostLobbyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display lobby label with translation key', () => {
    const lobbyLabel = fixture.nativeElement.querySelector('.lobby-label');
    expect(lobbyLabel.textContent).toContain('GAME.LOBBY_TITLE');
    expect(lobbyLabel.textContent).not.toContain('ABC12');
  });

  it('should show obscured room code by default', () => {
    const codeText = fixture.nativeElement.querySelector('.room-code-text');
    expect(codeText.textContent).toContain('•••••');
  });

  it('should reveal code when eye button is clicked', () => {
    const eyeBtn = fixture.nativeElement.querySelector('.room-code-area .icon-btn');
    eyeBtn.click();
    fixture.detectChanges();

    const codeText = fixture.nativeElement.querySelector('.room-code-text');
    expect(codeText.textContent).toContain('ABC12');
  });

  it('should toggle code visibility back to hidden on second click', () => {
    const eyeBtn = fixture.nativeElement.querySelector('.room-code-area .icon-btn');
    eyeBtn.click();
    fixture.detectChanges();
    eyeBtn.click();
    fixture.detectChanges();

    const codeText = fixture.nativeElement.querySelector('.room-code-text');
    expect(codeText.textContent).toContain('•••••');
  });

  it('should copy room code to clipboard and show tooltip when copy button is clicked', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });

    const copyBtn = fixture.nativeElement.querySelectorAll('.room-code-area .icon-btn')[1];
    copyBtn.click();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith('ABC12');
    expect(component.copiado()).toBe(true);
  });

  it('should show copy tooltip text after clicking copy', () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });

    expect(fixture.nativeElement.querySelector('.copy-tooltip')).toBeFalsy();

    const copyBtn = fixture.nativeElement.querySelectorAll('.room-code-area .icon-btn')[1];
    copyBtn.click();
    fixture.detectChanges();

    const tooltip = fixture.nativeElement.querySelector('.copy-tooltip');
    expect(tooltip).toBeTruthy();
    expect(tooltip.textContent).toBe('GAME.CODIGO_COPIADO');
  });

  it('should hide copy tooltip after timeout', () => {
    vi.useFakeTimers();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });

    const copyBtn = fixture.nativeElement.querySelectorAll('.room-code-area .icon-btn')[1];
    copyBtn.click();
    fixture.detectChanges();
    expect(component.copiado()).toBe(true);

    vi.advanceTimersByTime(2000);

    expect(component.copiado()).toBe(false);

    vi.useRealTimers();
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
      expect(component.canEdit({ connectionId: 'conn1', name: 'Player1', isHost: true, team: '', isReady: false })).toBe(true);
    });

    it('should return false when connectionId does not match', () => {
      expect(component.canEdit({ connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false })).toBe(false);
    });

    it('should return false when connectionId does not match for other player', () => {
      expect(component.canEdit({ connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false })).toBe(false);
    });
  });

  describe('template', () => {
    it('should show edit button for own player when canEdit is true', () => {
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
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

    it('should render round time slider with value from settings', () => {
      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      expect(slider).toBeTruthy();
      expect(Number(slider.value)).toBe(60);
      expect(slider.min).toBe('30');
      expect(slider.max).toBe('300');
    });

    it('should render number of rounds input with default value', () => {
      const roundInput = fixture.nativeElement.querySelector('.config-field input[type="number"]');
      expect(roundInput).toBeTruthy();
      expect(Number(roundInput.value)).toBe(4);
    });

    it('should render tooltip buttons for every settings field', () => {
      const tooltips = fixture.nativeElement.querySelectorAll('app-tooltip .tooltip-btn');
      expect(tooltips.length).toBeGreaterThan(5);
    });

    it('should render difficulty checkboxes from card options', () => {
      const diffBoxes = fixture.nativeElement.querySelectorAll('.checkbox-item input[type="checkbox"]');
      expect(diffBoxes.length).toBeGreaterThan(3);
    });

    it('should enable settings inputs for host', () => {
      const inputs = fixture.nativeElement.querySelectorAll('.config-field input, .config-field select');
      const disabled = Array.from(inputs as NodeListOf<HTMLInputElement | HTMLSelectElement>).filter(el => el.disabled);
      expect(disabled.length).toBe(0);
    });

    it('should disable settings inputs for non-host', () => {
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      const inputs = fixture.nativeElement.querySelectorAll('.config-field input, .config-field select');
      expect(inputs.length).toBeGreaterThan(0);
      const fields = Array.from(inputs as NodeListOf<HTMLInputElement | HTMLSelectElement>);
      const disabled = fields.filter(el => el.disabled);
      expect(disabled.length).toBe(fields.length);
    });

    it('should not render save button and hide read-only note for host', () => {
      expect(fixture.nativeElement.querySelector('.panel-config .btn-primary')).toBeFalsy();
      expect(fixture.nativeElement.querySelector('.panel-config .panel-note')).toBeFalsy();
    });

    it('should show read-only note for non-host', () => {
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.panel-config .btn-primary')).toBeFalsy();
      const note = fixture.nativeElement.querySelector('.panel-config .panel-note');
      expect(note.textContent).toContain('SETTINGS.READ_ONLY');
    });

    it('should pre-fill draft from cached settings signal', () => {
      mockGameService.settings.set({ ...createDefaultGameSettings(), roundTimeSeconds: 90 });
      fixture.detectChanges();

      expect(component.draft().roundTimeSeconds).toBe(90);
    });

    it('should compute fill percent for slider', () => {
      expect(component.fillPercent(0, 120, 60)).toBe('50%');
      expect(component.fillPercent(30, 120, 30)).toBe('0%');
      expect(component.fillPercent(30, 120, 120)).toBe('100%');
      expect(component.fillPercent(0, 15, 7.5)).toBe('50%');
      expect(component.fillPercent(30, 120, 999)).toBe('100%');
      expect(component.fillPercent(30, 120, 60)).toBe('33.33333333333333%');
      expect(component.fillPercent(30, 300, 165)).toBe('50%');
      expect(component.fillPercent(30, 300, 300)).toBe('100%');
    });

    it('should format duration with minutes conversion', () => {
      expect(component.formatDuration(30)).toBe('30s');
      expect(component.formatDuration(60)).toBe('1min');
      expect(component.formatDuration(90)).toBe('1min 30s');
      expect(component.formatDuration(300)).toBe('5min');
    });

    it('should render round time labels with minutes conversion', () => {
      const labels = fixture.nativeElement.querySelectorAll('.slider-row span');
      expect(labels[0].textContent).toContain('1min');
      expect(labels[1].textContent).toContain('5min');
    });

    it('should bind --fill custom property on range slider', () => {
      mockGameService.settings.set({ ...createDefaultGameSettings(), roundTimeSeconds: 165 });
      fixture.detectChanges();

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      expect(slider.style.getPropertyValue('--fill')).toBe('50%');
    });

    it('should autosave when slider is released', async () => {
      vi.useFakeTimers();
      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(mockGameService.configurarPartida).toHaveBeenCalledWith(component.draft());
      vi.useRealTimers();
    });

    it('should debounce rapid autosave calls', async () => {
      vi.useFakeTimers();
      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      slider.dispatchEvent(new Event('change'));
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(mockGameService.configurarPartida).toHaveBeenCalledTimes(1);
      vi.useRealTimers();
    });

    it('should not autosave when not host', async () => {
      vi.useFakeTimers();
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(mockGameService.configurarPartida).not.toHaveBeenCalled();
      vi.useRealTimers();
    });

    it('should show saved feedback after autosave completes', async () => {
      vi.useFakeTimers();
      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);
      fixture.detectChanges();

      expect(component.salvando()).toBe(false);
      expect(component.salvarFeedback()).toBe('SETTINGS.SALVO');
      const status = fixture.nativeElement.querySelector('.autosave-status');
      expect(status.textContent).toContain('SETTINGS.SALVO');
      vi.useRealTimers();
    });

    it('should show saving indicator while autosave runs', () => {
      vi.useFakeTimers();
      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      expect(component.salvando()).toBe(false);

      vi.advanceTimersByTime(400);
      expect(component.salvando()).toBe(true);
      vi.useRealTimers();
    });

    it('should revert only the invalid field to server state on save error', async () => {
      vi.useFakeTimers();
      mockGameService.configurarPartida.mockResolvedValue(null);
      component.draft.set({ ...createDefaultGameSettings(), numberOfRounds: 5, roundTimeSeconds: 90 });

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(component.salvarFeedback()).toBe('SETTINGS.ERRO');
      expect(component.draft().numberOfRounds).toBe(4);
      expect(component.draft().roundTimeSeconds).toBe(90);
      vi.useRealTimers();
    });

    it('should revert empty category list to server state on save error', async () => {
      vi.useFakeTimers();
      mockGameService.configurarPartida.mockResolvedValue(null);
      mockGameService.settings.set({ ...createDefaultGameSettings(), categories: ['Objeto', 'Tecnologia'] });

      component.draft.set({ ...createDefaultGameSettings(), categories: [] });

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(component.salvarFeedback()).toBe('SETTINGS.ERRO');
      expect(component.draft().categories).toEqual(['Objeto', 'Tecnologia']);
      vi.useRealTimers();
    });

    it('should revert invalid tipoo lead limit on save error', async () => {
      vi.useFakeTimers();
      mockGameService.configurarPartida.mockResolvedValue(null);
      component.draft.set({ ...createDefaultGameSettings(), tipooLeadLimit: 5 });

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(component.draft().tipooLeadLimit).toBeNull();
      vi.useRealTimers();
    });

    it('should keep draft unchanged when autosave succeeds', async () => {
      vi.useFakeTimers();
      component.draft.set({ ...createDefaultGameSettings(), roundTimeSeconds: 90 });

      const slider = fixture.nativeElement.querySelector('.config-field input[type="range"]');
      slider.dispatchEvent(new Event('change'));
      await vi.advanceTimersByTimeAsync(400);

      expect(component.draft().roundTimeSeconds).toBe(90);
      vi.useRealTimers();
    });

    it('should have a VOLTAR button with translation', () => {
      const backBtn = fixture.nativeElement.querySelector('.btn-secondary');
      expect(backBtn).toBeTruthy();
      expect(backBtn.textContent).toContain('GAME.VOLTAR');
    });

    it('should call sairDaSala and desconectar on navigateBack', async () => {
      await component.navigateBack();

      expect(mockGameService.sairDaSala).toHaveBeenCalled();
      expect(mockGameService.desconectar).toHaveBeenCalled();
    });
  });

  describe('inline edit', () => {
    it('should show input when edit button is clicked', () => {
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      expect(input).toBeTruthy();
      expect(input.value).toBe('Player1');
    });

    it('should call alterarNome and close input on confirm', () => {
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'NovoNome';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();

      expect(mockGameService.alterarNome).toHaveBeenCalledWith('NovoNome');
      expect(component.editingPlayerId()).toBeNull();
    });

    it('should save name on successful rename', async () => {
      mockGameService.alterarNome.mockResolvedValue(true);
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'SavedName';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();
      await new Promise(resolve => setTimeout(resolve));

      expect(mockPlayerStorage.saveName).toHaveBeenCalledWith('SavedName');
    });

    it('should not save name on failed rename', async () => {
      mockGameService.alterarNome.mockResolvedValue(false);
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'BadName';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();
      await new Promise(resolve => setTimeout(resolve));

      expect(mockPlayerStorage.saveName).not.toHaveBeenCalled();
    });

    it('should close input on Escape key', () => {
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.dispatchEvent(new KeyboardEvent('keyup', { key: 'Escape' }));

      expect(component.editingPlayerId()).toBeNull();
    });

    it('should show success tooltip when rename succeeds', async () => {
      mockGameService.alterarNome.mockResolvedValue(true);
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'NovoNome';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();
      await new Promise(resolve => setTimeout(resolve));
      fixture.detectChanges();

      const feedback = fixture.nativeElement.querySelector('.nome-feedback');
      expect(feedback).toBeTruthy();
      expect(feedback.classList).toContain('success');
      expect(feedback.textContent).toBe('GAME.NOME_ALTERADO');
    });

    it('should show error tooltip when rename fails', async () => {
      mockGameService.alterarNome.mockResolvedValue(false);
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'NomeRuim';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();
      await new Promise(resolve => setTimeout(resolve));
      fixture.detectChanges();

      const feedback = fixture.nativeElement.querySelector('.nome-feedback');
      expect(feedback).toBeTruthy();
      expect(feedback.classList).toContain('error');
      expect(feedback.textContent).toBe('GAME.NOME_INVALIDO');
    });

    it('should hide tooltip after timeout', async () => {
      vi.useFakeTimers();
      mockGameService.alterarNome.mockResolvedValue(true);
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = 'NovoNome';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();
      await Promise.resolve();
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.nome-feedback')).toBeTruthy();

      vi.advanceTimersByTime(2000);
      fixture.detectChanges();

      expect(fixture.nativeElement.querySelector('.nome-feedback')).toBeFalsy();

      vi.useRealTimers();
    });

    it('should not call alterarNome with empty name on confirm', () => {
      const editBtn = fixture.nativeElement.querySelector('[aria-label="Editar Nome"]');
      editBtn.click();
      fixture.detectChanges();

      const input = fixture.nativeElement.querySelector('.name-input');
      input.value = '   ';
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();

      const confirmBtn = fixture.nativeElement.querySelector('[aria-label="Confirmar nome"]');
      confirmBtn.click();

      expect(mockGameService.alterarNome).not.toHaveBeenCalled();
    });
  });

  describe('teams', () => {
    it('should show ENTRAR_TIME_VERMELHO text when not in red team', () => {
      const redCard = fixture.nativeElement.querySelector('.red-team h3');
      expect(redCard.textContent).toContain('GAME.ENTRAR_TIME_VERMELHO');
    });

    it('should show SAIR_TIME_VERMELHO when player is in red team', () => {
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: 'Vermelho', isReady: false },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false },
      ]);
      fixture.detectChanges();

      const redCard = fixture.nativeElement.querySelector('.red-team h3');
      expect(redCard.textContent).toContain('GAME.SAIR_TIME_VERMELHO');
    });

    it('should show SELECAO_ALEATORIA on randomize button', () => {
      const btn = fixture.nativeElement.querySelector('.randomize-btn');
      expect(btn.textContent).toContain('GAME.SELECAO_ALEATORIA');
    });

    it('should call escolherTime when clicking a team card', () => {
      const redCard = fixture.nativeElement.querySelector('.red-team');
      redCard.click();

      expect(mockGameService.escolherTime).toHaveBeenCalledWith('Vermelho');
    });

    it('should call randomizarTime when clicking randomize button', () => {
      const btn = fixture.nativeElement.querySelector('.randomize-btn');
      btn.click();

      expect(mockGameService.randomizarTime).toHaveBeenCalled();
    });

    it('should show ready players in team list with check icon', () => {
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: 'Vermelho', isReady: true },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: 'Azul', isReady: false },
      ]);
      fixture.detectChanges();

      const redListItems = fixture.nativeElement.querySelectorAll('.red-team .team-player-list li');
      expect(redListItems.length).toBe(1);
    });
  });

  describe('pronto / forcar iniciar', () => {
    it('should show FORCAR_INICIAR when player is host', () => {
      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      expect(readyBtn.textContent).toContain('GAME.FORCAR_INICIAR');
    });

    it('should show PRONTO when player is not host', () => {
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      expect(readyBtn.textContent).toContain('GAME.PRONTO');
    });

    it('should disable pronto button when not host and not in a team', () => {
      mockGameService.meuConnectionId.set('conn2');
      fixture.detectChanges();

      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      expect(readyBtn.disabled).toBe(true);
    });

    it('should enable pronto button when not host and in a team', () => {
      mockGameService.meuConnectionId.set('conn2');
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: 'Vermelho', isReady: false },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: 'Azul', isReady: false },
      ]);
      fixture.detectChanges();

      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      expect(readyBtn.disabled).toBe(false);
    });

    it('should call forcarIniciar when host clicks ready button', () => {
      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      readyBtn.click();

      expect(mockGameService.forcarIniciar).toHaveBeenCalled();
    });

    it('should show tooltip after forcarIniciar succeeds', async () => {
      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      readyBtn.click();
      await new Promise(resolve => setTimeout(resolve));
      fixture.detectChanges();

      const tooltip = fixture.nativeElement.querySelector('.start-tooltip');
      expect(tooltip).toBeTruthy();
      expect(tooltip.textContent).toContain('GAME.INICIANDO');
    });

    it('should call alternarPronto when non-host clicks ready button', () => {
      mockGameService.meuConnectionId.set('conn2');
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: 'Vermelho', isReady: false },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: 'Azul', isReady: false },
      ]);
      fixture.detectChanges();

      const readyBtn = fixture.nativeElement.querySelector('.ready-btn');
      readyBtn.click();

      expect(mockGameService.alternarPronto).toHaveBeenCalled();
    });

    it('should toggle pronto signal when alternarPronto succeeds', async () => {
      mockGameService.meuConnectionId.set('conn2');
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: 'Vermelho', isReady: false },
        { connectionId: 'conn2', name: 'Player2', isHost: false, team: 'Azul', isReady: false },
      ]);
      mockGameService.alternarPronto.mockResolvedValue(true);
      fixture.detectChanges();

      await component.togglePronto();

      expect(component.pronto()).toBe(true);
    });
  });
});
