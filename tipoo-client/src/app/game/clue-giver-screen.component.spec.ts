import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { ClueGiverScreenComponent } from './clue-giver-screen.component';
import { GameService } from '../services/game.service';
import { createDefaultGameSettings, GameSettings } from '../models/game-settings';

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
    players: ReturnType<typeof signal<{ connectionId: string; name: string; team?: string }[]>>;
    meuConnectionId: ReturnType<typeof signal<string>>;
    settings: ReturnType<typeof signal<GameSettings>>;
  };

  beforeEach(async () => {
    (globalThis as any).ResizeObserver = MockResizeObserver;

    mockGameService = {
      players: signal([]),
      meuConnectionId: signal('conn1'),
      settings: signal(settingsCustomizados({})),
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

  it('should start with the first card of the deck', () => {
    expect(component.carta().mainWord).toBe('CLIPE');
    expect(component.carta().forbidden.length).toBe(5);
  });

  it('should format time as MM:SS', () => {
    component.tempoRestante.set(185);
    expect(component.tempoFormatado()).toBe('03:05');
    component.tempoRestante.set(5);
    expect(component.tempoFormatado()).toBe('00:05');
  });

  it('should flag low time at or below 30 seconds', () => {
    component.tempoRestante.set(31);
    expect(component.tempoBaixo()).toBe(false);
    component.tempoRestante.set(30);
    expect(component.tempoBaixo()).toBe(true);
    component.tempoRestante.set(29);
    expect(component.tempoBaixo()).toBe(true);
  });

  describe('acertou', () => {
    it('should add one point to the current team', () => {
      const antes = component.pontos()['Vermelho'];
      component.acertou();
      expect(component.pontos()['Vermelho']).toBe(antes + 1);
    });

    it('should add one point to the round score', () => {
      component.acertou();
      expect(component.pontosRodada()).toBe(1);
    });

    it('should use pointsPerCorrect from the settings', () => {
      mockGameService.settings.set(settingsCustomizados({ pointsPerCorrect: 3 }));
      recriarComponente();
      component.acertou();
      expect(component.pontos()['Vermelho']).toBe(3);
      expect(component.pontosRodada()).toBe(3);
    });

    it('should advance to the next card', () => {
      const idx = component.cardIndex();
      component.acertou();
      expect(component.cardIndex()).toBe(idx + 1);
    });

    it('should register the play in history', () => {
      component.acertou();
      expect(component.historico()).toContain('ACERTOU • CLIPE');
    });
  });

  describe('pulou', () => {
    it('should decrement skip counter and advance card', () => {
      const idx = component.cardIndex();
      component.pulou();
      expect(component.pulosRestantes()).toBe(2);
      expect(component.cardIndex()).toBe(idx + 1);
    });

    it('should not skip when no skips are left', () => {
      component.pulosRestantes.set(0);
      const idx = component.cardIndex();
      component.pulou();
      expect(component.cardIndex()).toBe(idx);
      expect(component.historico()).toHaveLength(0);
    });

    it('should not deduct points when skipCostsPoints is disabled', () => {
      const antes = component.pontos()['Vermelho'];
      component.pulou();
      expect(component.pontos()['Vermelho']).toBe(antes);
      expect(component.pontosRodada()).toBe(0);
    });

    it('should deduct pointsPerSkip when skipCostsPoints is enabled', () => {
      mockGameService.settings.set(settingsCustomizados({ skipCostsPoints: true, pointsPerSkip: 2 }));
      recriarComponente();
      const antes = component.pontos()['Vermelho'];
      component.pulou();
      expect(component.pontos()['Vermelho']).toBe(antes - 2);
      expect(component.pontosRodada()).toBe(-2);
    });
  });

  describe('errou', () => {
    it('should subtract one point from the current team', () => {
      const antes = component.pontos()['Vermelho'];
      component.errou();
      expect(component.pontos()['Vermelho']).toBe(antes - 1);
    });

    it('should subtract one point from the round score', () => {
      component.errou();
      expect(component.pontosRodada()).toBe(-1);
    });

    it('should use pointsPerError from the settings', () => {
      mockGameService.settings.set(settingsCustomizados({ pointsPerError: 2 }));
      recriarComponente();
      component.errou();
      expect(component.pontos()['Vermelho']).toBe(-2);
      expect(component.pontosRodada()).toBe(-2);
    });

    it('should advance to the next card', () => {
      const idx = component.cardIndex();
      component.errou();
      expect(component.cardIndex()).toBe(idx + 1);
    });
  });

  describe('timer', () => {
    it('should end the round when the timer reaches zero', () => {
      vi.useFakeTimers();
      component.iniciarCronometro();
      component.tempoRestante.set(1);
      vi.advanceTimersByTime(1000);
      expect(component.fase()).toBe('fimRodada');
      vi.useRealTimers();
    });

    it('should stop counting down after the round ends', () => {
      vi.useFakeTimers();
      component.iniciarCronometro();
      component.tempoRestante.set(1);
      vi.advanceTimersByTime(1000);
      component.tempoRestante.set(5);
      vi.advanceTimersByTime(3000);
      expect(component.tempoRestante()).toBe(5);
      vi.useRealTimers();
    });
  });

  describe('proximaRodada', () => {
    it('should switch team and reset round state', () => {
      component.fase.set('fimRodada');
      component.pulosRestantes.set(0);
      component.pontosRodada.set(5);
      component.proximaRodada();
      expect(component.timeAtual()).toBe('Azul');
      expect(component.rodadaAtual()).toBe(2);
      expect(component.pulosRestantes()).toBe(component.limitePulos());
      expect(component.fase()).toBe('jogando');
      expect(component.tempoRestante()).toBe(180);
      expect(component.pontosRodada()).toBe(0);
    });

    it('should end the match after the last round', () => {
      component.rodadaAtual.set(component.totalRodadas());
      component.fase.set('fimRodada');
      component.proximaRodada();
      expect(component.fase()).toBe('fimPartida');
    });
  });

  describe('novaPartida', () => {
    it('should reset all state', () => {
      component.pontos.set({ Vermelho: 5, Azul: 3 });
      component.pontosRodada.set(4);
      component.rodadaAtual.set(4);
      component.cardIndex.set(6);
      component.pulosRestantes.set(0);
      component.fase.set('fimPartida');

      component.novaPartida();

      expect(component.pontos()).toEqual({ Vermelho: 0, Azul: 0 });
      expect(component.pontosRodada()).toBe(0);
      expect(component.rodadaAtual()).toBe(1);
      expect(component.cardIndex()).toBe(0);
      expect(component.pulosRestantes()).toBe(component.limitePulos());
      expect(component.fase()).toBe('jogando');
      expect(component.timeAtual()).toBe('Vermelho');
    });
  });

  describe('config', () => {
    it('should use roundTimeSeconds to init the timer and progress', () => {
      mockGameService.settings.set(settingsCustomizados({ roundTimeSeconds: 240 }));
      recriarComponente();
      expect(component.tempoRestante()).toBe(240);
      expect(component.tempoTotal()).toBe(240);
      component.tempoRestante.set(120);
      expect(component.progresso()).toBe(50);
    });

    it('should use numberOfRounds as the total', () => {
      mockGameService.settings.set(settingsCustomizados({ numberOfRounds: 8 }));
      recriarComponente();
      expect(component.totalRodadas()).toBe(8);
    });

    it('should use skipLimit as the initial skips', () => {
      mockGameService.settings.set(settingsCustomizados({ skipLimit: 5 }));
      recriarComponente();
      expect(component.pulosRestantes()).toBe(5);
      expect(component.limitePulos()).toBe(5);
    });

    it('should reset the timer with roundTimeSeconds after proximaRodada', () => {
      mockGameService.settings.set(settingsCustomizados({ roundTimeSeconds: 300 }));
      recriarComponente();
      component.fase.set('fimRodada');
      component.tempoRestante.set(10);
      component.proximaRodada();
      expect(component.tempoRestante()).toBe(300);
    });

    it('should start on the blue team when startingTeam is azul', () => {
      mockGameService.settings.set(settingsCustomizados({ startingTeam: 'azul' }));
      recriarComponente();
      expect(component.timeAtual()).toBe('Azul');
    });

    it('should start on the red team when startingTeam is vermelho', () => {
      mockGameService.settings.set(settingsCustomizados({ startingTeam: 'vermelho' }));
      recriarComponente();
      expect(component.timeAtual()).toBe('Vermelho');
    });

    it('should clamp roundTimeSeconds to the allowed range', () => {
      mockGameService.settings.set(settingsCustomizados({ roundTimeSeconds: 99999 }));
      recriarComponente();
      expect(component.tempoTotal()).toBe(600);
    });

    it('should clamp skipLimit to the allowed range', () => {
      mockGameService.settings.set(settingsCustomizados({ skipLimit: 99 }));
      recriarComponente();
      expect(component.limitePulos()).toBe(10);
    });

    it('should clamp pointsPerSkip to the allowed range', () => {
      mockGameService.settings.set(settingsCustomizados({ skipCostsPoints: true, pointsPerSkip: 99 }));
      recriarComponente();
      component.pulou();
      expect(component.pontos()['Vermelho']).toBe(-10);
    });

    it('should force an even number of rounds when clamping', () => {
      mockGameService.settings.set(settingsCustomizados({ numberOfRounds: 3 }));
      recriarComponente();
      expect(component.totalRodadas()).toBe(4);
    });
  });

  describe('template', () => {
    it('should render the main word and forbidden words', () => {
      const word = fixture.nativeElement.querySelector('.main-word');
      expect(word.textContent).toContain('CLIPE');
      const forbidden = fixture.nativeElement.querySelectorAll('.forbidden-list li');
      expect(forbidden.length).toBe(5);
    });

    it('should apply the fit-text directive to the main word', () => {
      const word = fixture.nativeElement.querySelector('.main-word');
      expect(word.style.whiteSpace).toBe('nowrap');
    });

    it('should not render difficulty or category badges', () => {
      expect(fixture.nativeElement.querySelector('.card-meta')).toBeFalsy();
      expect(fixture.nativeElement.textContent).not.toContain('Fácil');
      expect(fixture.nativeElement.textContent).not.toContain('Objeto');
    });

    it('should show the round card counter', () => {
      const count = fixture.nativeElement.querySelector('.card-count');
      expect(count).toBeTruthy();
      expect(count.textContent).toContain('1ª');
    });

    it('should not render the X icon inside forbidden words', () => {
      const xs = fixture.nativeElement.querySelectorAll('.forbidden-list li .x-icon');
      expect(xs.length).toBe(0);
    });

    it('should update round score after acertou', () => {
      component.acertou();
      fixture.detectChanges();
      const score = fixture.nativeElement.querySelector('.score-value');
      expect(score.textContent).toContain('1');
    });

    it('should show the player name and team in the top badge', () => {
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'João' },
        { connectionId: 'conn2', name: 'Maria' },
      ]);
      fixture.detectChanges();
      const badge = fixture.nativeElement.querySelector('.team-badge');
      expect(badge.textContent).toContain('João');
      expect(badge.textContent).toContain('Vermelho');
    });

    it('should show a fallback name when the player is not found', () => {
      fixture.detectChanges();
      const badge = fixture.nativeElement.querySelector('.team-badge');
      expect(badge.textContent).toContain('Jogador');
    });

    it('should apply team-blue to the shell when the player is on the blue team', () => {
      mockGameService.players.set([
        { connectionId: 'conn1', name: 'João', team: 'Azul' },
      ]);
      fixture.detectChanges();
      const shell = fixture.nativeElement.querySelector('.game-shell');
      expect(shell.classList.contains('team-blue')).toBe(true);
      expect(shell.classList.contains('team-red')).toBe(false);
    });

    it('should constrain history panel height without growing the page', () => {
      const list = fixture.nativeElement.querySelector('.history-list');
      expect(getComputedStyle(list).overflowY).toBe('auto');
    });

    it('should disable skip button when no skips are left', () => {
      component.pulosRestantes.set(0);
      fixture.detectChanges();
      const skipBtn = fixture.nativeElement.querySelector('.skip-btn');
      expect(skipBtn.disabled).toBe(true);
    });

    it('should place the round points in the left panel', () => {
      const panel = fixture.nativeElement.querySelector('.pontos-da-rodada');
      expect(panel).toBeTruthy();
      const left = fixture.nativeElement.querySelector('.left-panel');
      expect(left).toContain(panel);
    });

    it('should render the acertou/errou test buttons next to the timer', () => {
      const timerWrap = fixture.nativeElement.querySelector('.timer-wrap');
      expect(timerWrap.querySelector('.test-btn.ok')).toBeTruthy();
      expect(timerWrap.querySelector('.test-btn.err')).toBeTruthy();
    });

    it('should hide the timer when panicMode is enabled', () => {
      mockGameService.settings.set(settingsCustomizados({ panicMode: true }));
      recriarComponente();
      expect(fixture.nativeElement.querySelector('.timer-ring')).toBeFalsy();
      expect(fixture.nativeElement.querySelector('.test-btn.ok')).toBeTruthy();
    });

    it('should render the round counter with total rounds from settings', () => {
      mockGameService.settings.set(settingsCustomizados({ numberOfRounds: 8 }));
      recriarComponente();
      const round = fixture.nativeElement.querySelector('.round-pill strong');
      expect(round.textContent).toContain('1 / 8');
    });

    it('should auto-scroll the history list to the last play', () => {
      const list = fixture.nativeElement.querySelector('.history-list');
      const nativeEl = component['historyList']?.nativeElement;
      expect(nativeEl).toBe(list);
      component.acertou();
      fixture.detectChanges();
      expect(list.scrollTop).toBe(list.scrollHeight);
    });

    it('should show round over screen when fase is fimRodada', () => {
      component.fase.set('fimRodada');
      fixture.detectChanges();
      expect(fixture.nativeElement.querySelector('.end-round-card')).toBeTruthy();
      expect(fixture.nativeElement.querySelector('.main-word')).toBeFalsy();
    });

    it('should show match over screen when fase is fimPartida', () => {
      component.fase.set('fimPartida');
      fixture.detectChanges();
      const text = fixture.nativeElement.querySelector('.end-round-card').textContent;
      expect(text).toContain('GAME.FIM_PARTIDA');
    });
  });
});
