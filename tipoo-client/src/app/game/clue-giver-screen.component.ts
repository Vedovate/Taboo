import { Component, computed, effect, inject, signal, ViewChild, ElementRef } from '@angular/core';
import { UpperCasePipe } from '@angular/common';
import { Router } from '@angular/router';
import {
  LucideCheck,
  LucideX,
  LucideSkipForward,
  LucideClock,
  LucideRotateCcw,
  LucideTrophy,
  LucideVolume2,
  LucideSend,
  LucideFlame,
  LucideVote,
  LucideSparkles,
  LucideAlertTriangle,
  LucideHourglass,
  LucideHome
} from '@lucide/angular';
import { TranslatePipe } from '../pipes/translate.pipe';
import { FitTextDirective } from './fit-text.directive';
import { GameService } from '../services/game.service';
import { Card } from '../models/card';

const clamp = (v: number, min: number, max: number): number => Math.min(Math.max(v, min), max);

const MOCK_CARD: Card = {
  id: 1,
  mainWord: 'CLIPE',
  forbidden: ['papel', 'escritório', 'grampo', 'metal', 'junto'],
  difficulty: 'Fácil',
  category: 'Objeto'
};

type Time = 'Vermelho' | 'Azul';

@Component({
  standalone: true,
  selector: 'app-clue-giver-screen',
  imports: [
    UpperCasePipe,
    LucideCheck,
    LucideX,
    LucideSkipForward,
    LucideClock,
    LucideRotateCcw,
    LucideTrophy,
    LucideVolume2,
    LucideSend,
    LucideFlame,
    LucideVote,
    LucideSparkles,
    LucideAlertTriangle,
    LucideHourglass,
    LucideHome,
    TranslatePipe,
    FitTextDirective
  ],
  templateUrl: './clue-giver-screen.component.html',
  styleUrls: ['./clue-giver-screen.component.scss'],
})
export class ClueGiverScreenComponent {
  readonly palpiteInput = signal('');
  readonly localTimer = signal(180);
  readonly localExplanationTimer = signal(5);
  readonly somBuzinaAtivo = signal(false);

  readonly gameService = inject(GameService);
  private readonly router = inject(Router);

  @ViewChild('chatList') private chatList?: ElementRef<HTMLElement>;
  @ViewChild('historyList') private historyList?: ElementRef<HTMLElement>;

  private intervalTimer: ReturnType<typeof setInterval> | null = null;
  private explanationInterval: ReturnType<typeof setInterval> | null = null;
  private somTimeout: ReturnType<typeof setTimeout> | null = null;

  // Estado sincronizado com backend / fallback
  readonly estado = computed(() => this.gameService.gameState());
  readonly fase = computed(() => this.estado()?.phase ?? 'jogando');
  readonly papel = computed(() => this.gameService.meuPapel());
  readonly jogadorAtual = computed(() => this.gameService.meuJogador());
  readonly nomeJogador = computed(() => this.jogadorAtual()?.name || 'Jogador');

  readonly timeAtual = computed<Time>(() =>
    (this.estado()?.activeTeam as Time) || (this.jogadorAtual()?.team as Time) || 'Vermelho'
  );

  readonly timeJogador = computed<Time>(() =>
    (this.jogadorAtual()?.team as Time) || this.timeAtual()
  );

  readonly rodadaAtual = computed(() => this.estado()?.roundNumber ?? 1);
  readonly totalRodadas = computed(() => this.estado()?.totalRounds ?? this.config().numberOfRounds);
  readonly pulosRestantes = computed(() => this.estado()?.skipsLeft ?? this.config().skipLimit);
  readonly pontosRodada = computed(() => this.estado()?.roundScore ?? 0);
  readonly pontosVermelho = computed(() => this.estado()?.scoreRed ?? 0);
  readonly pontosAzul = computed(() => this.estado()?.scoreBlue ?? 0);
  readonly explicadorNome = computed(() => this.estado()?.spokespersonName || this.nomeJogador());

  readonly carta = computed<Card>(() => {
    return this.estado()?.currentCard || MOCK_CARD;
  });

  readonly cartasRodada = computed(() => this.gameService.roundCards());
  readonly chatMensagens = computed(() => this.gameService.chatMessages());
  readonly buzinaAtiva = computed(() => this.gameService.activeBuzzer());
  readonly estatisticasFinais = computed(() => this.gameService.endStats());
  readonly alertaEmpate = computed(() => this.gameService.empateSorteadoAlerta());

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
      explanationTimeSeconds: clamp(s.explanationTimeSeconds, 0, 15),
      panicMode: s.panicMode,
      startingTeam: s.startingTeam,
    };
  });

  readonly tempoTotal = computed(() => this.config().roundTimeSeconds);
  readonly panicMode = computed(() => this.config().panicMode);
  readonly tempoRestante = computed(() => this.localTimer());

  readonly tempoFormatado = computed(() => {
    const s = Math.max(this.localTimer(), 0);
    const min = Math.floor(s / 60);
    const seg = s % 60;
    return `${String(min).padStart(2, '0')}:${String(seg).padStart(2, '0')}`;
  });

  readonly tempoBaixo = computed(() => this.localTimer() <= 30 && this.fase() === 'jogando');
  readonly progresso = computed(() => (this.localTimer() / this.tempoTotal()) * 100);

  // Reanálise e Julgamento
  readonly contestedCardIndexes = computed(() => this.estado()?.contestedCardIndexes ?? []);
  readonly currentJudgingIndex = computed(() => this.estado()?.currentJudgingIndex ?? 0);
  readonly readyToAdvancePlayerIds = computed(() => this.estado()?.readyToAdvancePlayerIds ?? []);
  readonly phaseTimeRemaining = computed(() => this.estado()?.phaseTimeRemaining ?? 20);
  readonly jogadores = computed(() => this.gameService.players());

  readonly souHost = computed(() =>
    this.jogadores().some(p => p.connectionId === this.gameService.meuConnectionId() && p.isHost)
  );

  readonly estouProntoTransicao = computed(() =>
    this.readyToAdvancePlayerIds().includes(this.gameService.meuConnectionId())
  );

  readonly cartaEmJulgamento = computed(() => {
    const indices = this.contestedCardIndexes();
    const curIdx = this.currentJudgingIndex();
    if (indices.length === 0 || curIdx >= indices.length) return null;
    const cardIndex = indices[curIdx];
    return this.cartasRodada().find(c => c.cardIndex === cardIndex) || null;
  });

  constructor() {
    this.localTimer.set(this.tempoTotal());
    this.iniciarCronometro();

    // Auto-scroll para histórico e chat
    effect(() => {
      this.chatMensagens();
      const el = this.chatList?.nativeElement;
      if (el) {
        setTimeout(() => { el.scrollTop = el.scrollHeight; }, 50);
      }
    });

    effect(() => {
      this.cartasRodada();
      const el = this.historyList?.nativeElement;
      if (el) {
        setTimeout(() => { el.scrollTop = el.scrollHeight; }, 50);
      }
    });

    // Sincroniza timer local quando o estado do jogo muda de rodada
    effect(() => {
      const st = this.estado();
      if (st) {
        if (st.phase === 'jogando' && this.localTimer() === 0) {
          this.localTimer.set(st.timeRemaining || this.tempoTotal());
        }
      }
    });

    // Controle de contagem regressiva da explicação pós-buzina
    effect(() => {
      const bz = this.buzinaAtiva();
      if (bz && this.fase() === 'explicacao_buzina') {
        this.tocarEfeitoBuzina();
        this.localExplanationTimer.set(bz.explanationTimeSeconds || 5);
        this.iniciarTimerExplicacao();
      } else {
        this.pararTimerExplicacao();
      }
    });
  }

  iniciarCronometro(): void {
    this.stopTimer();
    this.intervalTimer = setInterval(() => {
      if (this.fase() === 'jogando') {
        this.localTimer.update(v => {
          if (v <= 1) {
            this.encerrarRodada();
            return 0;
          }
          return v - 1;
        });
      }
    }, 1000);
  }

  private stopTimer(): void {
    if (this.intervalTimer) {
      clearInterval(this.intervalTimer);
      this.intervalTimer = null;
    }
  }

  private iniciarTimerExplicacao(): void {
    this.pararTimerExplicacao();
    this.explanationInterval = setInterval(() => {
      this.localExplanationTimer.update(v => {
        if (v <= 1) {
          this.fecharModalExplicacao();
          return 0;
        }
        return v - 1;
      });
    }, 1000);
  }

  private pararTimerExplicacao(): void {
    if (this.explanationInterval) {
      clearInterval(this.explanationInterval);
      this.explanationInterval = null;
    }
  }

  private tocarEfeitoBuzina(): void {
    this.somBuzinaAtivo.set(true);
    if (this.somTimeout) clearTimeout(this.somTimeout);
    this.somTimeout = setTimeout(() => this.somBuzinaAtivo.set(false), 2000);
  }

  async acertou(): Promise<void> {
    await this.gameService.acertarCarta();
  }

  async pulou(): Promise<void> {
    if (this.pulosRestantes() <= 0) return;
    await this.gameService.pularCarta();
  }

  async errou(): Promise<void> {
    await this.gameService.buzinar('Palavra Proibida', 'Erro Manual');
  }

  async buzinarPalavra(palavra: string, tipo = 'Palavra Proibida'): Promise<void> {
    await this.gameService.buzinar(palavra, tipo);
  }

  async enviarPalpite(): Promise<void> {
    const texto = this.palpiteInput().trim();
    if (!texto) return;
    this.palpiteInput.set('');
    await this.gameService.enviarPalpite(texto);
  }

  async alternarContestacao(cardIndex: number, contestar: boolean): Promise<void> {
    await this.gameService.marcarCartaParaJulgamento(cardIndex, contestar);
  }

  isCardContested(cardIndex: number): boolean {
    return this.contestedCardIndexes().includes(cardIndex);
  }

  async confirmarSelecaoReanalise(): Promise<void> {
    await this.gameService.confirmarSelecaoReanalise();
  }

  async votarJulgamento(cardIndex: number, opcao: string): Promise<void> {
    await this.gameService.votarJulgamentoCarta(cardIndex, opcao);
  }

  async votar(cardIndex: number, opcao: string): Promise<void> {
    await this.votarJulgamento(cardIndex, opcao);
  }

  meuVotoNaCarta(carta: Card | any): string | null {
    if (!carta?.playerVotes) return null;
    return carta.playerVotes[this.gameService.meuConnectionId()] || null;
  }

  async confirmarProntoTransicao(): Promise<void> {
    await this.gameService.confirmarProntoTransicao();
  }

  async fecharModalExplicacao(): Promise<void> {
    this.pararTimerExplicacao();
    await this.gameService.finalizarTempoExplicacao();
  }

  async encerrarRodada(): Promise<void> {
    this.stopTimer();
    await this.gameService.finalizarRodada();
  }

  async proximaRodada(): Promise<void> {
    this.localTimer.set(this.tempoTotal());
    this.iniciarCronometro();
    await this.gameService.avancarRodada();
  }

  async forcarAvancarRodada(): Promise<void> {
    if (!this.souHost()) return;
    await this.proximaRodada();
  }

  async novaPartida(): Promise<void> {
    this.localTimer.set(this.tempoTotal());
    this.iniciarCronometro();
    await this.gameService.reiniciarPartida();
  }

  async voltarAoLobby(): Promise<void> {
    this.stopTimer();
    this.router.navigate(['/lobby']);
  }
}
