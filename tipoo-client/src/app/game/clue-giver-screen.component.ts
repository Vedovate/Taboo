import { Component, computed, effect, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { LucideCheck, LucideX, LucideSkipForward, LucideClock, LucideRotateCcw, LucideTrophy, LucideVolume2 } from '@lucide/angular';
import { TranslatePipe } from '../pipes/translate.pipe';
import { FitTextDirective } from './fit-text.directive';
import { GameService } from '../services/game.service';
import { createDefaultGameSettings } from '../models/game-settings';

const clamp = (v: number, min: number, max: number): number => Math.min(Math.max(v, min), max);

interface MockCard {
  mainWord: string;
  forbidden: string[];
  difficulty: string;
  category: string;
}

const MOCK_DECK: MockCard[] = [
  { mainWord: 'CLIPE', forbidden: ['papel', 'escritório', 'grampo', 'metal', 'junto'], difficulty: 'Fácil', category: 'Objeto' },
  { mainWord: 'SOFTWARE', forbidden: ['programa', 'computador', 'instalar', 'CD-ROM', 'linguagem'], difficulty: 'Fácil', category: 'Tecnologia' },
  { mainWord: 'INTELIGENTE', forbidden: ['burro', 'esperto', 'intelectual', 'brilhante', 'estúpido'], difficulty: 'Médio', category: 'Adjetivo' },
  { mainWord: 'ÂNCORA', forbidden: ['navio', 'barco', 'noticiário', 'jogar', 'içar'], difficulty: 'Fácil', category: 'Objeto' },
  { mainWord: 'PRISÃO', forbidden: ['cadeia', 'grades', 'cárcere', 'cela', 'criminoso'], difficulty: 'Fácil', category: 'Local' },
  { mainWord: 'ROXO', forbidden: ['cor', 'azul', 'violeta', 'raiva', 'lavanda'], difficulty: 'Fácil', category: 'Cor' },
  { mainWord: 'MARACUJÁ', forbidden: ['rugas', 'azedo', 'semente', 'fruta', 'amarelo'], difficulty: 'Fácil', category: 'Alimento' },
  { mainWord: 'AUSTRÁLIA', forbidden: ['canguru', 'Sidnei', 'coala', 'Dundee', 'Oceania'], difficulty: 'Médio', category: 'Geografia' },
];

type Time = 'Vermelho' | 'Azul';
type Fase = 'jogando' | 'fimRodada' | 'fimPartida';

@Component({
  standalone: true,
  selector: 'app-clue-giver-screen',
  imports: [LucideCheck, LucideX, LucideSkipForward, LucideClock, LucideRotateCcw, LucideTrophy, LucideVolume2, TranslatePipe, FitTextDirective],
  templateUrl: './clue-giver-screen.component.html',
  styleUrls: ['./clue-giver-screen.component.scss'],
})
export class ClueGiverScreenComponent {
  readonly cardIndex = signal(0);
  readonly tempoRestante = signal(0);
  readonly rodadaAtual = signal(1);
  readonly pulosRestantes = signal(0);
  readonly cartasNaRodada = signal(0);
  readonly timeAtual = signal<Time>('Vermelho');
  readonly pontos = signal<Record<Time, number>>({ Vermelho: 0, Azul: 0 });
  readonly pontosRodada = signal(0);
  readonly fase = signal<Fase>('jogando');
  readonly historico = signal<string[]>([]);
  private readonly gameService = inject(GameService);
  @ViewChild('historyList') private historyList?: ElementRef<HTMLElement>;
  private timer: ReturnType<typeof setInterval> | null = null;

  readonly config = computed(() => {
    const s = this.gameService.settings();
    const rounds = clamp(s.numberOfRounds, 2, 20);
    return {
      roundTimeSeconds: clamp(s.roundTimeSeconds, 30, 600),
      numberOfRounds: rounds % 2 === 0 ? rounds : rounds + 1,
      skipLimit: clamp(s.skipLimit, 0, 10),
      skipCostsPoints: s.skipCostsPoints,
      pointsPerCorrect: clamp(s.pointsPerCorrect, 0, 10),
      pointsPerError: clamp(s.pointsPerError, 0, 10),
      pointsPerSkip: clamp(s.pointsPerSkip, 0, 10),
      panicMode: s.panicMode,
      startingTeam: s.startingTeam,
    };
  });
  readonly totalRodadas = computed(() => this.config().numberOfRounds);
  readonly limitePulos = computed(() => this.config().skipLimit);
  readonly tempoTotal = computed(() => this.config().roundTimeSeconds);
  readonly panicMode = computed(() => this.config().panicMode);

  readonly jogadorAtual = computed(() =>
    this.gameService.players().find(p => p.connectionId === this.gameService.meuConnectionId()),
  );
  readonly nomeJogador = computed(() => this.jogadorAtual()?.name || 'Jogador');
  readonly timeJogador = computed<Time>(() => (this.jogadorAtual()?.team as Time) || this.timeAtual());

  readonly carta = computed(() => MOCK_DECK[this.cardIndex() % MOCK_DECK.length]);
  readonly tempoFormatado = computed(() => {
    const s = Math.max(this.tempoRestante(), 0);
    const min = Math.floor(s / 60);
    const seg = s % 60;
    return `${String(min).padStart(2, '0')}:${String(seg).padStart(2, '0')}`;
  });
  readonly tempoBaixo = computed(() => this.tempoRestante() <= 30 && this.fase() === 'jogando');
  readonly pontosTime = (time: Time) => this.pontos()[time];
  readonly progresso = computed(() => (this.tempoRestante() / this.tempoTotal()) * 100);

  constructor() {
    this.tempoRestante.set(this.tempoTotal());
    this.pulosRestantes.set(this.limitePulos());
    this.timeAtual.set(this.resolveStartingTeam());
    this.iniciarCronometro();
    effect(() => {
      this.historico();
      const el = this.historyList?.nativeElement;
      if (el) {
        el.scrollTop = el.scrollHeight;
      }
    });
  }

  private resolveStartingTeam(): Time {
    const start = this.config().startingTeam;
    if (start === 'azul') {
      return 'Azul';
    }
    if (start === 'vermelho') {
      return 'Vermelho';
    }
    return Math.random() < 0.5 ? 'Vermelho' : 'Azul';
  }

  iniciarCronometro(): void {
    this.stopTimer();
    this.timer = setInterval(() => {
      this.tempoRestante.update(v => {
        if (v <= 1) {
          this.encerrarRodada();
          return 0;
        }
        return v - 1;
      });
    }, 1000);
  }

  private stopTimer(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }

  proximaCarta(): void {
    this.cardIndex.update(i => i + 1);
    this.cartasNaRodada.update(c => c + 1);
  }

  acertou(): void {
    const pts = this.config().pointsPerCorrect;
    this.pontos.update(p => ({ ...p, [this.timeAtual()]: p[this.timeAtual()] + pts }));
    this.pontosRodada.update(v => v + pts);
    this.historico.update(h => [...h, `ACERTOU • ${this.carta().mainWord}`]);
    this.proximaCarta();
  }

  pulou(): void {
    if (this.pulosRestantes() <= 0) {
      return;
    }
    this.pulosRestantes.update(p => p - 1);
    if (this.config().skipCostsPoints) {
      const pts = this.config().pointsPerSkip;
      this.pontos.update(p => ({ ...p, [this.timeAtual()]: p[this.timeAtual()] - pts }));
      this.pontosRodada.update(v => v - pts);
    }
    this.historico.update(h => [...h, `PASSOU • ${this.carta().mainWord}`]);
    this.proximaCarta();
  }

  errou(): void {
    const pts = this.config().pointsPerError;
    this.pontos.update(p => ({ ...p, [this.timeAtual()]: p[this.timeAtual()] - pts }));
    this.pontosRodada.update(v => v - pts);
    this.historico.update(h => [...h, `ERROU • ${this.carta().mainWord}`]);
    this.proximaCarta();
  }

  encerrarRodada(): void {
    this.stopTimer();
    this.fase.set('fimRodada');
  }

  proximaRodada(): void {
    const fim = this.rodadaAtual() >= this.totalRodadas();
    if (fim) {
      this.fase.set('fimPartida');
      return;
    }
    this.rodadaAtual.update(r => r + 1);
    this.timeAtual.update(t => (t === 'Vermelho' ? 'Azul' : 'Vermelho'));
    this.pulosRestantes.set(this.limitePulos());
    this.cartasNaRodada.set(0);
    this.pontosRodada.set(0);
    this.tempoRestante.set(this.tempoTotal());
    this.fase.set('jogando');
    this.iniciarCronometro();
  }

  novaPartida(): void {
    this.cardIndex.set(0);
    this.rodadaAtual.set(1);
    this.pulosRestantes.set(this.limitePulos());
    this.cartasNaRodada.set(0);
    this.timeAtual.set(this.resolveStartingTeam());
    this.pontos.set({ Vermelho: 0, Azul: 0 });
    this.pontosRodada.set(0);
    this.historico.set([]);
    this.tempoRestante.set(this.tempoTotal());
    this.fase.set('jogando');
    this.iniciarCronometro();
  }
}
