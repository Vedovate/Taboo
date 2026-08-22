import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, computed, Signal } from '@angular/core';
import { ClueGiverScreenComponent } from './clue-giver-screen.component';
import { GameService } from '../services/game.service';
import { createDefaultGameSettings, GameSettings } from '../models/game-settings';
import { GameState } from '../models/game-state';

class MockResizeObserver {
  observe(): void {}
  disconnect(): void {}
  unobserve(): void {}
}

function settingsCustomizados(patch: Partial<GameSettings>): GameSettings {
  return { ...createDefaultGameSettings(), startingTeam: 'vermelho', ...patch };
}

describe('ClueGiverScreenComponent', () => {
  let component: ClueGiverScreenComponent;
  let fixture: ComponentFixture<ClueGiverScreenComponent>;
  let mockGameService: {
    players: ReturnType<typeof signal<{ connectionId: string; name: string; team?: string; isHost?: boolean; isReady?: boolean }[]>>;
    meuConnectionId: ReturnType<typeof signal<string>>;
    settings: ReturnType<typeof signal<GameSettings>>;
    gameState: ReturnType<typeof signal<GameState | null>>;
    roundCards: Signal<any[]>;
    chatMessages: Signal<any[]>;
    activeBuzzer: Signal<any>;
    endStats: Signal<any>;
    empateSorteadoAlerta: ReturnType<typeof signal<boolean>>;
    meuPapel: ReturnType<typeof signal<string>>;
    meuJogador: ReturnType<typeof signal<any>>;
    acertarCarta: () => Promise<void>;
    pularCarta: () => Promise<void>;
    buzinar: (palavra: string, tipo: string) => Promise<void>;
    enviarPalpite: (texto: string) => Promise<void>;
    votarCarta: (index: number, opcao: string) => Promise<void>;
    marcarCartaParaJulgamento: (index: number, contestar: boolean) => Promise<void>;
    confirmarSelecaoReanalise: () => Promise<void>;
    votarJulgamentoCarta: (index: number, opcao: string) => Promise<void>;
    confirmarProntoTransicao: () => Promise<void>;
    finalizarTempoExplicacao: () => Promise<void>;
    finalizarRodada: () => Promise<void>;
    avancarRodada: () => Promise<void>;
    reiniciarPartida: () => Promise<void>;
  };

  beforeEach(async () => {
    (globalThis as any).ResizeObserver = MockResizeObserver;

    const gameStateSignal = signal<GameState | null>(null);
    mockGameService = {
      players: signal([{ connectionId: 'conn1', name: 'João', team: 'Vermelho', isHost: true, isReady: true }]),
      meuConnectionId: signal('conn1'),
      settings: signal(settingsCustomizados({})),
      gameState: gameStateSignal,
      roundCards: computed(() => gameStateSignal()?.roundCards ?? []),
      chatMessages: computed(() => gameStateSignal()?.chatMessages ?? []),
      activeBuzzer: computed(() => gameStateSignal()?.activeBuzzer ?? null),
      endStats: computed(() => gameStateSignal()?.endStats ?? null),
      empateSorteadoAlerta: signal(false),
      meuPapel: signal('explicador'),
      meuJogador: signal({ connectionId: 'conn1', name: 'João', team: 'Vermelho' }),
      acertarCarta: vi.fn().mockResolvedValue(undefined),
      pularCarta: vi.fn().mockResolvedValue(undefined),
      buzinar: vi.fn().mockResolvedValue(undefined),
      enviarPalpite: vi.fn().mockResolvedValue(undefined),
      votarCarta: vi.fn().mockResolvedValue(undefined),
      marcarCartaParaJulgamento: vi.fn().mockResolvedValue(undefined),
      confirmarSelecaoReanalise: vi.fn().mockResolvedValue(undefined),
      votarJulgamentoCarta: vi.fn().mockResolvedValue(undefined),
      confirmarProntoTransicao: vi.fn().mockResolvedValue(undefined),
      finalizarTempoExplicacao: vi.fn().mockResolvedValue(undefined),
      finalizarRodada: vi.fn().mockResolvedValue(undefined),
      avancarRodada: vi.fn().mockResolvedValue(undefined),
      reiniciarPartida: vi.fn().mockResolvedValue(undefined),
    };

    await TestBed.configureTestingModule({
      imports: [ClueGiverScreenComponent],
      providers: [{ provide: GameService, useValue: mockGameService }],
    }).compileComponents();

    fixture = TestBed.createComponent(ClueGiverScreenComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  function recriarComponente(): void {
    component['stopTimer']();
    fixture.destroy();
    fixture = TestBed.createComponent(ClueGiverScreenComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  afterEach(() => {
    component['stopTimer']();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display the card and forbidden words for clue giver', () => {
    expect(component.carta().mainWord).toBe('CLIPE');
    expect(component.carta().forbidden.length).toBe(5);
  });

  it('should format time as MM:SS', () => {
    component.localTimer.set(185);
    expect(component.tempoFormatado()).toBe('03:05');
    component.localTimer.set(5);
    expect(component.tempoFormatado()).toBe('00:05');
  });

  it('should flag low time at or below 30 seconds', () => {
    component.localTimer.set(31);
    expect(component.tempoBaixo()).toBe(false);
    component.localTimer.set(30);
    expect(component.tempoBaixo()).toBe(true);
    component.localTimer.set(29);
    expect(component.tempoBaixo()).toBe(true);
  });

  describe('acertou', () => {
    it('should call gameService.acertarCarta', async () => {
      await component.acertou();
      expect(mockGameService.acertarCarta).toHaveBeenCalled();
    });
  });

  describe('pulou', () => {
    it('should call gameService.pularCarta when skips available', async () => {
      await component.pulou();
      expect(mockGameService.pularCarta).toHaveBeenCalled();
    });

    it('should not call gameService.pularCarta when no skips available', async () => {
      mockGameService.gameState.set({
        roomCode: 'S1',
        roundNumber: 1,
        totalRounds: 6,
        activeTeam: 'Vermelho',
        spokespersonId: 'conn1',
        spokespersonName: 'João',
        currentCard: null,
        scoreRed: 0,
        scoreBlue: 0,
        roundScore: 0,
        phase: 'jogando',
        skipsLeft: 0,
        timeRemaining: 180,
        activeBuzzer: null,
        roundCards: [],
        chatMessages: [],
        endStats: null,
      });
      fixture.detectChanges();
      await component.pulou();
      expect(mockGameService.pularCarta).not.toHaveBeenCalled();
    });
  });

  describe('buzinar', () => {
    it('should call gameService.buzinar with word and type', async () => {
      await component.buzinarPalavra('papel', 'Palavra Proibida');
      expect(mockGameService.buzinar).toHaveBeenCalledWith('papel', 'Palavra Proibida');
    });
  });

  describe('enviarPalpite', () => {
    it('should call gameService.enviarPalpite and clear input', async () => {
      component.palpiteInput.set('Grampo');
      await component.enviarPalpite();
      expect(mockGameService.enviarPalpite).toHaveBeenCalledWith('Grampo');
      expect(component.palpiteInput()).toBe('');
    });
  });

  describe('votar', () => {
    it('should call gameService.votarJulgamentoCarta with index and option', async () => {
      await component.votarJulgamento(0, 'reverter');
      expect(mockGameService.votarJulgamentoCarta).toHaveBeenCalledWith(0, 'reverter');
    });

    it('should call gameService.marcarCartaParaJulgamento on alternarContestacao', async () => {
      await component.alternarContestacao(0, true);
      expect(mockGameService.marcarCartaParaJulgamento).toHaveBeenCalledWith(0, true);
    });

    it('should call gameService.confirmarSelecaoReanalise on confirmarSelecaoReanalise', async () => {
      await component.confirmarSelecaoReanalise();
      expect(mockGameService.confirmarSelecaoReanalise).toHaveBeenCalled();
    });

    it('should call gameService.confirmarProntoTransicao on confirmarProntoTransicao', async () => {
      await component.confirmarProntoTransicao();
      expect(mockGameService.confirmarProntoTransicao).toHaveBeenCalled();
    });
  });

  describe('proximaRodada e novaPartida', () => {
    it('should call gameService.avancarRodada', async () => {
      await component.proximaRodada();
      expect(mockGameService.avancarRodada).toHaveBeenCalled();
    });

    it('should call gameService.reiniciarPartida', async () => {
      await component.novaPartida();
      expect(mockGameService.reiniciarPartida).toHaveBeenCalled();
    });
  });

  describe('template rendering', () => {
    it('should render the main word and forbidden words in clue giver view', () => {
      const word = fixture.nativeElement.querySelector('.main-word');
      expect(word.textContent).toContain('CLIPE');
      const forbidden = fixture.nativeElement.querySelectorAll('.forbidden-list li');
      expect(forbidden.length).toBe(5);
    });

    it('should render the team badge with player name', () => {
      const badge = fixture.nativeElement.querySelector('.team-badge');
      expect(badge.textContent).toContain('João');
      expect(badge.textContent).toContain('VERMELHO');
    });

    it('should render watcher view when papel is vigia', () => {
      mockGameService.meuPapel.set('vigia');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.watcher-grid')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.buzzer-target-btn')).toBeTruthy();
    });

    it('should render guesser view when papel is adivinhador', () => {
      mockGameService.meuPapel.set('adivinhador');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.guesser-grid')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.guess-field')).toBeTruthy();
    });

    it('should render reanalysis screen when fase is selecao_reanalise', () => {
      mockGameService.gameState.set({
        roomCode: 'S1',
        roundNumber: 1,
        totalRounds: 6,
        activeTeam: 'Vermelho',
        spokespersonId: 'conn1',
        spokespersonName: 'João',
        currentCard: null,
        scoreRed: 3,
        scoreBlue: 2,
        roundScore: 3,
        phase: 'selecao_reanalise',
        skipsLeft: 3,
        timeRemaining: 0,
        activeBuzzer: null,
        roundCards: [
          {
            cardIndex: 0,
            cardId: 1,
            mainWord: 'CLIPE',
            forbidden: ['papel', 'escritório', 'grampo', 'metal', 'junto'],
            status: 'Acertou',
            votesKeep: 1,
            votesReverse: 0,
            votesCancel: 0,
            votingStatus: 'none',
            wasTiebreakRandomized: false,
          }
        ],
        chatMessages: [],
        endStats: null,
      });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.reanalysis-screen')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.carousel-card')).toBeTruthy();
    });

    it('should render judging screen when fase is julgamento_carta', () => {
      mockGameService.gameState.set({
        roomCode: 'S1',
        roundNumber: 1,
        totalRounds: 6,
        activeTeam: 'Vermelho',
        spokespersonId: 'conn1',
        spokespersonName: 'João',
        currentCard: null,
        scoreRed: 3,
        scoreBlue: 2,
        roundScore: 3,
        phase: 'julgamento_carta',
        skipsLeft: 3,
        timeRemaining: 0,
        activeBuzzer: null,
        contestedCardIndexes: [0],
        currentJudgingIndex: 0,
        roundCards: [
          {
            cardIndex: 0,
            cardId: 1,
            mainWord: 'CLIPE',
            forbidden: ['papel', 'escritório', 'grampo', 'metal', 'junto'],
            status: 'Errou',
            votesKeep: 0,
            votesReverse: 0,
            votesCancel: 0,
            votingStatus: 'none',
            wasTiebreakRandomized: false,
          }
        ],
        chatMessages: [],
        endStats: null,
      });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.judging-screen')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.strong-vote-btn')).toBeTruthy();
    });

    it('should render round summary screen when fase is resumo_rodada', () => {
      mockGameService.gameState.set({
        roomCode: 'S1',
        roundNumber: 1,
        totalRounds: 6,
        activeTeam: 'Vermelho',
        spokespersonId: 'conn1',
        spokespersonName: 'João',
        currentCard: null,
        scoreRed: 3,
        scoreBlue: 2,
        roundScore: 3,
        phase: 'resumo_rodada',
        skipsLeft: 3,
        timeRemaining: 0,
        activeBuzzer: null,
        roundCards: [],
        chatMessages: [],
        endStats: null,
      });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.round-summary-screen')).toBeTruthy();
    });

    it('should render game over screen when fase is fim_partida', () => {
      mockGameService.gameState.set({
        roomCode: 'S1',
        roundNumber: 6,
        totalRounds: 6,
        activeTeam: 'Vermelho',
        spokespersonId: 'conn1',
        spokespersonName: 'João',
        currentCard: null,
        scoreRed: 10,
        scoreBlue: 8,
        roundScore: 0,
        phase: 'fim_partida',
        skipsLeft: 0,
        timeRemaining: 0,
        activeBuzzer: null,
        roundCards: [],
        chatMessages: [],
        endStats: {
          winnerTeam: 'Vermelho',
          scoreRed: 10,
          scoreBlue: 8,
          totalRounds: 6,
          mvpName: 'João',
          mvpPoints: 5,
          topBuzzerName: 'Maria',
          topBuzzerCount: 3,
          mostBuzzedName: 'Pedro',
          mostBuzzedCount: 2,
          fastestCardWord: 'CLIPE',
          fastestCardSeconds: 3,
          totalCorrect: 10,
          totalErrors: 4,
          totalSkips: 2,
          totalContestedReversed: 1,
        },
      });
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.gameover-screen')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.winner-title').textContent).toContain('VITÓRIA DO TIME VERMELHO');
      expect(fixture.nativeElement.querySelector('.mvp-card').textContent).toContain('João');
    });
  });
});
