import { Card } from './card';
import { PlayedCard } from './played-card';
import { BuzzerEvent } from './buzzer-event';
import { ChatMessage } from './chat-message';
import { GameEndStats } from './game-end-stats';

export interface GameState {
  roomCode: string;
  roundNumber: number;
  totalRounds: number;
  activeTeam: 'Vermelho' | 'Azul' | string;
  spokespersonId: string;
  spokespersonName: string;
  currentCard: Card | null;
  scoreRed: number;
  scoreBlue: number;
  roundScore: number;
  phase: 'jogando' | 'explicacao_buzina' | 'selecao_reanalise' | 'julgamento_carta' | 'resumo_rodada' | 'revisao' | 'fim_partida' | string;
  skipsLeft: number;
  timeRemaining: number;
  activeBuzzer: BuzzerEvent | null;
  roundCards: PlayedCard[];
  chatMessages: ChatMessage[];
  endStats: GameEndStats | null;
  currentJudgingIndex?: number;
  contestedCardIndexes?: number[];
  readyToAdvancePlayerIds?: string[];
  phaseTimeRemaining?: number;
}
