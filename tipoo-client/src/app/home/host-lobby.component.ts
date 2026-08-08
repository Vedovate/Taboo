import { Component, computed, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideUserX, LucideEye, LucideEyeOff, LucideCopy, LucidePencil, LucideCheck, LucideSettings } from '@lucide/angular';
import { GameService } from '../services/game.service';
import { PlayerStorageService } from '../services/player-storage.service';
import { LobbyPlayer } from '../models/lobby-player';
import { TranslatePipe } from '../pipes/translate.pipe';
import { MatchSettingsModalComponent } from '../components/match-settings-modal/match-settings-modal.component';

@Component({
  standalone: true,
  selector: 'app-host-lobby',
  imports: [LucideUserX, LucideEye, LucideEyeOff, LucideCopy, LucidePencil, LucideCheck, LucideSettings, TranslatePipe, MatchSettingsModalComponent],
  templateUrl: './host-lobby.component.html',
  styleUrls: ['./host-lobby.component.scss'],
})
export class HostLobbyComponent {
  readonly showCode = signal(false);
  readonly copiado = signal(false);
  readonly editingPlayerId = signal<string | null>(null);
  readonly editNomeValue = signal('');
  readonly nomeFeedback = signal<{ key: string; success: boolean } | null>(null);
  private copiadoTimeout: ReturnType<typeof setTimeout> | null = null;
  private feedbackTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly pronto = signal(false);
  readonly forcarIniciarTooltip = signal(false);
  private tooltipTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly mostrarModal = signal(false);

  readonly souHost = computed(() =>
    this.gameService.players().some(p => p.connectionId === this.gameService.meuConnectionId() && p.isHost)
  );

  constructor(
    private router: Router,
    public gameService: GameService,
    private playerStorage: PlayerStorageService,
  ) {}

  canEdit(player: LobbyPlayer): boolean {
    return player.connectionId === this.gameService.meuConnectionId();
  }

  async navigateBack(): Promise<void> {
    await this.gameService.sairDaSala();
    await this.gameService.desconectar();
    this.router.navigate(['/']);
  }

  toggleCodeVisibility(): void {
    this.showCode.update(v => !v);
  }

  copiarCodigo(): void {
    navigator.clipboard.writeText(this.gameService.roomCode());
    this.copiado.set(true);
    if (this.copiadoTimeout) clearTimeout(this.copiadoTimeout);
    this.copiadoTimeout = setTimeout(() => this.copiado.set(false), 2000);
  }

  iniciarEdicao(player: LobbyPlayer): void {
    this.editNomeValue.set(player.name);
    this.editingPlayerId.set(player.connectionId);
  }

  async confirmarNome(): Promise<void> {
    const novo = this.editNomeValue().trim();
    this.editingPlayerId.set(null);
    if (!novo) {
      return;
    }
    const sucesso = await this.gameService.alterarNome(novo);
    if (sucesso) {
      this.playerStorage.saveName(novo);
    }
    this.mostrarFeedback(sucesso);
  }

  private mostrarFeedback(sucesso: boolean): void {
    this.nomeFeedback.set({
      key: sucesso ? 'GAME.NOME_ALTERADO' : 'GAME.NOME_INVALIDO',
      success: sucesso,
    });
    if (this.feedbackTimeout) clearTimeout(this.feedbackTimeout);
    this.feedbackTimeout = setTimeout(() => this.nomeFeedback.set(null), 2000);
  }

  cancelarNome(): void {
    this.editingPlayerId.set(null);
  }

  expulsarJogador(connectionId: string): void {
    this.gameService.expulsarJogador(connectionId);
  }

  jogadoresDoTime(cor: string): LobbyPlayer[] {
    return this.gameService.players().filter(p => p.team === cor);
  }

  estouNoTime(cor: string): boolean {
    return this.gameService.players().some(
      p => p.connectionId === this.gameService.meuConnectionId() && p.team === cor
    );
  }

  get prontoDesabilitado(): boolean {
    return !this.gameService.players().some(
      p => p.connectionId === this.gameService.meuConnectionId() && p.team
    );
  }

  async entrarTime(cor: string): Promise<void> {
    await this.gameService.escolherTime(cor);
  }

  async randomizarTime(): Promise<void> {
    await this.gameService.randomizarTime();
  }

  async togglePronto(): Promise<void> {
    if (this.souHost()) {
      const ok = await this.gameService.forcarIniciar();
      if (ok) {
        this.forcarIniciarTooltip.set(true);
        if (this.tooltipTimeout) clearTimeout(this.tooltipTimeout);
        this.tooltipTimeout = setTimeout(() => this.forcarIniciarTooltip.set(false), 2500);
      }
      return;
    }
    const novoEstado = await this.gameService.alternarPronto();
    this.pronto.set(novoEstado);
  }
}
