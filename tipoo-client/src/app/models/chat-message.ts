export interface ChatMessage {
  id: string;
  connectionId: string;
  authorName: string;
  team: string;
  message: string;
  isGuess: boolean;
  isCorrect: boolean;
  timestamp: string;
}
