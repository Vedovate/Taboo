export interface PlayedCard {
  cardIndex: number;
  cardId: number;
  mainWord: string;
  forbidden: string[];
  status: 'Acertou' | 'Errou' | 'Pulou' | 'Anulada' | string;
  buzzedByName?: string;
  infractionWord?: string;
  infractionType?: string;
  votesKeep: number;
  votesReverse: number;
  votesCancel: number;
  votingStatus: 'none' | 'voting' | 'resolved' | string;
  wasTiebreakRandomized: boolean;
  isContested?: boolean;
  playerVotes?: Record<string, string>;
}
