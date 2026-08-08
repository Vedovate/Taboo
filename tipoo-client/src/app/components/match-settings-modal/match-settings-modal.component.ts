import { Component, computed, effect, output, signal } from '@angular/core';
import { LucideX } from '@lucide/angular';
import { GameService } from '../../services/game.service';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { TooltipComponent } from '../../shared/tooltip/tooltip.component';
import {
  BUZZER_SOUNDS,
  createDefaultGameSettings,
  DEFAULT_DIFFICULTIES,
  DEFAULT_TIPOO_LEAD_LIMIT,
  GameSettings,
  STARTING_TEAM_OPTIONS,
  TIEBREAK_OPTIONS,
} from '../../models/game-settings';

@Component({
  standalone: true,
  selector: 'app-match-settings-modal',
  imports: [LucideX, TranslatePipe, TooltipComponent],
  templateUrl: './match-settings-modal.component.html',
  styleUrls: ['./match-settings-modal.component.scss'],
})
export class MatchSettingsModalComponent {
  readonly fechar = output<void>();

  readonly draft = signal<GameSettings>(createDefaultGameSettings());
  readonly salvando = signal(false);
  readonly salvarFeedback = signal<string | null>(null);
  readonly erros = signal<Partial<Record<keyof GameSettings, string>>>({});
  readonly haErros = computed(() => Object.keys(this.erros()).length > 0);
  private salvarFeedbackTimeout: ReturnType<typeof setTimeout> | null = null;

  private readonly LIMITES: Partial<Record<keyof GameSettings, { min: number; max: number; par?: boolean }>> = {
    numberOfRounds: { min: 2, max: 20, par: true },
    skipLimit: { min: 0, max: 10 },
    tipooLeadLimit: { min: 10, max: 999999 },
    pointsPerCorrect: { min: 0, max: 10 },
    pointsPerError: { min: 0, max: 10 },
    pointsPerSkip: { min: 0, max: 10 },
  };

  readonly souHost = computed(() =>
    this.gameService.players().some(p => p.connectionId === this.gameService.meuConnectionId() && p.isHost)
  );

  readonly opcoesDificuldades = computed(() => {
    const opcoes = this.gameService.cardOptions().dificuldades;
    const base = opcoes.length > 0 ? opcoes : DEFAULT_DIFFICULTIES;
    const ordenadas = DEFAULT_DIFFICULTIES.filter(d => base.includes(d));
    const extras = base.filter(d => !DEFAULT_DIFFICULTIES.includes(d));
    return [...ordenadas, ...extras];
  });

  readonly startingTeamOptions = STARTING_TEAM_OPTIONS;
  readonly tiebreakOptions = TIEBREAK_OPTIONS;
  readonly buzzerSoundOptions = BUZZER_SOUNDS;
  readonly tipooAtivo = computed(() => this.draft().tipooLeadLimit !== null);
  readonly buzinasVazias = computed(() => this.draft().buzzerSounds.length === 0);

  private readonly settingsSync = effect(() => {
    this.draft.set({ ...this.gameService.settings() });
    this.erros.set({});
  });

  constructor(public gameService: GameService) {}

  cancelar(): void {
    if (this.salvando()) {
      return;
    }
    this.fechar.emit();
  }

  async salvar(): Promise<void> {
    if (!this.souHost() || this.salvando()) {
      return;
    }
    if (!this.validarTudo()) {
      this.reverterCamposInvalidos(this.gameService.settings());
      this.mostrarErro();
      return;
    }
    this.salvando.set(true);
    try {
      const resultado = await this.gameService.configurarPartida(this.draft());
      if (resultado) {
        this.mostrarSucesso();
      } else {
        this.reverterCamposInvalidos(this.gameService.settings());
        this.mostrarErro();
      }
    } catch {
      this.reverterCamposInvalidos(this.gameService.settings());
      this.mostrarErro();
    } finally {
      this.salvando.set(false);
    }
  }

  private mostrarErro(): void {
    this.salvarFeedback.set('SETTINGS.ERRO');
    if (this.salvarFeedbackTimeout) clearTimeout(this.salvarFeedbackTimeout);
    this.salvarFeedbackTimeout = setTimeout(() => this.salvarFeedback.set(null), 2500);
  }

  private mostrarSucesso(): void {
    this.salvarFeedback.set('SETTINGS.SALVO');
    if (this.salvarFeedbackTimeout) clearTimeout(this.salvarFeedbackTimeout);
    this.salvarFeedbackTimeout = setTimeout(() => this.fechar.emit(), 700);
  }

  setNumberField(field: keyof GameSettings, event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = input.value.trim();
    const limite = this.LIMITES[field];

    if (limite) {
      if (raw === '') {
        this.erros.update(e => ({ ...e, [field]: 'SETTINGS.ERRORS.REQUIRED' }));
        return;
      }
      const num = Number(raw);
      if (!Number.isInteger(num)) {
        this.erros.update(e => ({ ...e, [field]: 'SETTINGS.ERRORS.INTEGER' }));
        return;
      }
      if (num < limite.min || num > limite.max) {
        this.erros.update(e => ({ ...e, [field]: 'SETTINGS.ERRORS.RANGE' }));
        return;
      }
      if (limite.par && num % 2 !== 0) {
        this.erros.update(e => ({ ...e, [field]: 'SETTINGS.ERRORS.EVEN' }));
        return;
      }
      this.draft.update(s => ({ ...s, [field]: num }) as GameSettings);
      this.limparErro(field);
      return;
    }

    this.draft.update(s => ({ ...s, [field]: Number(raw) }) as GameSettings);
  }

  onBlurNumero(field: keyof GameSettings, event: Event): void {
    if (!this.erros()[field]) {
      return;
    }
    const input = event.target as HTMLInputElement;
    input.value = String(this.draft()[field]);
    this.limparErro(field);
  }

  private limparErro(field: keyof GameSettings): void {
    this.erros.update(e => {
      const rest = { ...e };
      delete rest[field];
      return rest;
    });
  }

  setBooleanField(field: keyof GameSettings, event: Event): void {
    const value = (event.target as HTMLInputElement).checked;
    this.draft.update(s => ({ ...s, [field]: value }) as GameSettings);
  }

  setSelectField(field: 'startingTeam' | 'tiebreakMode', event: Event): void {
    const value = (event.target as HTMLSelectElement).value as GameSettings[typeof field];
    this.draft.update(s => ({ ...s, [field]: value }) as GameSettings);
  }

  toggleListField(field: 'difficulties' | 'buzzerSounds', value: string, event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.draft.update(s => {
      const current = [...s[field]];
      const idx = current.indexOf(value);
      if (checked && idx === -1) {
        current.push(value);
      } else if (!checked && idx !== -1) {
        if (field === 'difficulties' && current.length === 1) {
          return s;
        }
        current.splice(idx, 1);
      }
      return { ...s, [field]: current } as GameSettings;
    });
  }

  inList(field: 'difficulties' | 'buzzerSounds', value: string): boolean {
    return this.draft()[field].includes(value);
  }

  isDificuldadeDesabilitada(d: string): boolean {
    return this.draft().difficulties.length === 1 && this.inList('difficulties', d);
  }

  setTipooAtivo(event: Event): void {
    const checked = (event.target as HTMLInputElement).checked;
    this.draft.update(s => ({
      ...s,
      tipooLeadLimit: checked ? (s.tipooLeadLimit ?? DEFAULT_TIPOO_LEAD_LIMIT) : null,
    }) as GameSettings);
    if (!checked) {
      this.limparErro('tipooLeadLimit');
    }
  }

  private validarTudo(): boolean {
    const s = this.draft();
    return this.validarCampo('numberOfRounds', s) && this.validarCampo('tipooLeadLimit', s) && this.validarCampo('difficulties', s);
  }

  private validarCampo(field: keyof GameSettings, s: GameSettings): boolean {
    if (field === 'tipooLeadLimit') {
      return s.tipooLeadLimit === null || (s.tipooLeadLimit >= 10 && s.tipooLeadLimit <= 999999);
    }
    const limite = this.LIMITES[field];
    if (limite) {
      const v = s[field] as number;
      return v >= limite.min && v <= limite.max && (!limite.par || v % 2 === 0);
    }
    if (field === 'difficulties') {
      return s.difficulties.length > 0;
    }
    return true;
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
    if (!this.validarCampo('difficulties', atual)) {
      corrigido.difficulties = [...anteriores.difficulties];
    }

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
