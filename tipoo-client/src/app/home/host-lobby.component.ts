import { Component, computed, effect, signal } from '@angular/core';
import { Router } from '@angular/router';
import { LucideUserX, LucideEye, LucideEyeOff, LucideCopy, LucidePencil, LucideCheck } from '@lucide/angular';
import { GameService } from '../services/game.service';
import { PlayerStorageService } from '../services/player-storage.service';
import { LobbyPlayer } from '../models/lobby-player';
import { TranslatePipe } from '../pipes/translate.pipe';
import { TooltipComponent } from '../shared/tooltip/tooltip.component';
import {
  BUZZER_SOUNDS,
  createDefaultGameSettings,
  DEFAULT_DIFFICULTIES,
  DEFAULT_TIPOO_LEAD_LIMIT,
  GameSettings,
  STARTING_TEAM_OPTIONS,
  TIEBREAK_OPTIONS,
} from '../models/game-settings';

@Component({
  standalone: true,
  selector: 'app-host-lobby',
  imports: [LucideUserX, LucideEye, LucideEyeOff, LucideCopy, LucidePencil, LucideCheck, TranslatePipe, TooltipComponent],
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

  readonly draft = signal<GameSettings>(createDefaultGameSettings());
  readonly salvando = signal(false);
  readonly salvarFeedback = signal<string | null>(null);
  private autosaveTimer: ReturnType<typeof setTimeout> | null = null;
  private salvarFeedbackTimeout: ReturnType<typeof setTimeout> | null = null;

  readonly souHost = computed(() =>
    this.gameService.players().some(p => p.connectionId === this.gameService.meuConnectionId() && p.isHost)
  );

  readonly opcoesDificuldades = computed(() => {
    const opcoes = this.gameService.cardOptions().dificuldades;
    return opcoes.length > 0 ? opcoes : DEFAULT_DIFFICULTIES;
  });

  readonly opcoesCategorias = computed(() => this.gameService.cardOptions().categorias);

  readonly startingTeamOptions = STARTING_TEAM_OPTIONS;
  readonly tiebreakOptions = TIEBREAK_OPTIONS;
  readonly buzzerSoundOptions = BUZZER_SOUNDS;
  readonly tipooAtivo = computed(() => this.draft().tipooLeadLimit !== null);

  private readonly settingsSync = effect(() => {
    this.draft.set({ ...this.gameService.settings() });
  });

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

  setNumberField(field: keyof GameSettings, event: Event): void {
    const value = Number((event.target as HTMLInputElement).value);
    this.draft.update(s => ({ ...s, [field]: value }) as GameSettings);
  }

  setBooleanField(field: keyof GameSettings, event: Event): void {
    const value = (event.target as HTMLInputElement).checked;
    this.draft.update(s => ({ ...s, [field]: value }) as GameSettings);
  }

  setSelectField(field: 'startingTeam' | 'tiebreakMode', event: Event): void {
    const value = (event.target as HTMLSelectElement).value as GameSettings[typeof field];
    this.draft.update(s => ({ ...s, [field]: value }) as GameSettings);
  }

  toggleListField(field: 'difficulties' | 'categories' | 'buzzerSounds', value: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.draft.update(s => {
      const current = [...s[field]];
      const idx = current.indexOf(value);
      if (checked && idx === -1) {
        current.push(value);
      } else if (!checked && idx !== -1) {
        current.splice(idx, 1);
      }
      return { ...s, [field]: current } as GameSettings;
    });
  }

  inList(field: 'difficulties' | 'categories' | 'buzzerSounds', value: string): boolean {
    return this.draft()[field].includes(value);
  }

  setTipooAtivo(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.draft.update(s => ({
      ...s,
      tipooLeadLimit: checked ? (s.tipooLeadLimit ?? DEFAULT_TIPOO_LEAD_LIMIT) : null,
    }) as GameSettings);
  }

  autosave(): void {
    if (!this.souHost() || this.salvando()) {
      return;
    }
    if (this.autosaveTimer) clearTimeout(this.autosaveTimer);
    this.autosaveTimer = setTimeout(() => this.executarAutosave(), 400);
  }

  private async executarAutosave(): Promise<void> {
    this.salvando.set(true);
    try {
      const resultado = await this.gameService.configurarPartida(this.draft());
      if (resultado) {
        this.salvarFeedback.set('SETTINGS.SALVO');
      } else {
        this.reverterCamposInvalidos(this.gameService.settings());
        this.salvarFeedback.set('SETTINGS.ERRO');
      }
    } catch {
      this.reverterCamposInvalidos(this.gameService.settings());
      this.salvarFeedback.set('SETTINGS.ERRO');
    } finally {
      this.salvando.set(false);
      if (this.salvarFeedbackTimeout) clearTimeout(this.salvarFeedbackTimeout);
      this.salvarFeedbackTimeout = setTimeout(() => this.salvarFeedback.set(null), 2500);
    }
  }

  private validarCampo(field: keyof GameSettings, s: GameSettings): boolean {
    switch (field) {
      case 'numberOfRounds':
        return s.numberOfRounds >= 2 && s.numberOfRounds <= 20 && s.numberOfRounds % 2 === 0;
      case 'tipooLeadLimit':
        return s.tipooLeadLimit === null || s.tipooLeadLimit >= 10;
      case 'difficulties':
      case 'categories':
      case 'buzzerSounds':
        return s[field].length > 0;
      default:
        return true;
    }
  }

  private reverterCamposInvalidos(anteriores: GameSettings): void {
    const atual = this.draft();
    const corrigido: GameSettings = { ...atual };

    if (!this.validarCampo('numberOfRounds', atual)) {
      corrigido.numberOfRounds = anteriores.numberOfRounds;
    }
    if (!this.validarCampo('tipooLeadLimit', atual)) {
      corrigido.tipooLeadLimit = anteriores.tipooLeadLimit;
    }
    (['difficulties', 'categories', 'buzzerSounds'] as const).forEach(field => {
      if (!this.validarCampo(field, atual)) {
        corrigido[field] = [...anteriores[field]];
      }
    });

    this.draft.set(corrigido);
  }

  fillPercent(min: number, max: number, value: number): string {
    const clamped = Math.min(Math.max(value, min), max);
    return `${((clamped - min) / (max - min)) * 100}%`;
  }

  formatDuration(seconds: number): string {
    if (seconds < 60) {
      return `${seconds}s`;
    }
    const minutes = Math.floor(seconds / 60);
    const rest = seconds % 60;
    return rest === 0 ? `${minutes}min` : `${minutes}min ${rest}s`;
  }
}
