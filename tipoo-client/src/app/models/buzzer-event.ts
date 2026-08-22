export interface BuzzerEvent {
  buzzedByConnectionId: string;
  buzzedByName: string;
  buzzerTeam: string;
  infractionWord: string;
  infractionType: string;
  explanationTimeSeconds: number;
}
