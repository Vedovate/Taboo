export interface GameEndStats {
  winnerTeam: string;
  scoreRed: number;
  scoreBlue: number;
  totalRounds: number;
  mvpName: string;
  mvpPoints: number;
  topBuzzerName: string;
  topBuzzerCount: number;
  mostBuzzedName: string;
  mostBuzzedCount: number;
  fastestCardWord: string;
  fastestCardSeconds: number;
  totalCorrect: number;
  totalErrors: number;
  totalSkips: number;
  totalContestedReversed: number;
}
