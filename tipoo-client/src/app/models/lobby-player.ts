export interface LobbyPlayer {
  connectionId: string;
  name: string;
  isHost: boolean;
  team: string;
  isReady: boolean;
}
