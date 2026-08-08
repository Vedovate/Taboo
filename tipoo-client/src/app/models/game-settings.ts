export interface GameSettings {
  roundTimeSeconds: number;
  numberOfRounds: number;
  skipLimit: number;
  skipCostsPoints: boolean;
  tipooLeadLimit: number | null;
  explanationTimeSeconds: number;
  difficulties: string[];
  buzzerSounds: string[];
  randomBuzzerSound: boolean;
  panicMode: boolean;
  pointsPerCorrect: number;
  pointsPerError: number;
  pointsPerSkip: number;
  startingTeam: 'azul' | 'vermelho' | 'aleatorio';
  tiebreakMode: 'empatado' | 'rodada-extra';
  pauseBetweenRoundsSeconds: number;
}

export const DEFAULT_DIFFICULTIES = ['Fácil', 'Médio', 'Difícil'];
export const BUZZER_SOUNDS = ['air-horn', 'censura', 'erro'];
export const STARTING_TEAM_OPTIONS = ['azul', 'vermelho', 'aleatorio'] as const;
export const TIEBREAK_OPTIONS = ['empatado', 'rodada-extra'] as const;
export const DEFAULT_TIPOO_LEAD_LIMIT = 100;

export function createDefaultGameSettings(): GameSettings {
  return {
    roundTimeSeconds: 180,
    numberOfRounds: 6,
    skipLimit: 3,
    skipCostsPoints: false,
    tipooLeadLimit: null,
    explanationTimeSeconds: 5,
    difficulties: [...DEFAULT_DIFFICULTIES],
    buzzerSounds: [...BUZZER_SOUNDS],
    randomBuzzerSound: true,
    panicMode: false,
    pointsPerCorrect: 1,
    pointsPerError: 1,
    pointsPerSkip: 1,
    startingTeam: 'aleatorio',
    tiebreakMode: 'empatado',
    pauseBetweenRoundsSeconds: 30,
  };
}
