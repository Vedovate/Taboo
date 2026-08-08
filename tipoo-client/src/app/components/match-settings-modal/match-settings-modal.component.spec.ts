import { ComponentFixture, TestBed } from '@angular/core/testing';
import { signal, WritableSignal } from '@angular/core';
import { MatchSettingsModalComponent } from './match-settings-modal.component';
import { GameService } from '../../services/game.service';
import { CardOptions } from '../../models/card-options';
import { createDefaultGameSettings, GameSettings } from '../../models/game-settings';

interface MockGameService {
  players: WritableSignal<{ connectionId: string; name: string; isHost: boolean; team: string; isReady: boolean }[]>;
  meuConnectionId: WritableSignal<string>;
  settings: WritableSignal<GameSettings>;
  cardOptions: WritableSignal<CardOptions>;
  configurarPartida: ReturnType<typeof vi.fn>;
}

describe('MatchSettingsModalComponent', () => {
  let component: MatchSettingsModalComponent;
  let fixture: ComponentFixture<MatchSettingsModalComponent>;
  let mockGameService: MockGameService;

  beforeEach(async () => {
    mockGameService = {
      players: signal([
        { connectionId: 'conn1', name: 'Player1', isHost: true, team: '', isReady: false },
      ]),
      meuConnectionId: signal('conn1'),
      settings: signal(createDefaultGameSettings()),
      cardOptions: signal({ dificuldades: ['Fácil', 'Médio', 'Difícil'], categorias: ['Objeto', 'Tecnologia', 'Conceito'] }),
      configurarPartida: vi.fn().mockResolvedValue(createDefaultGameSettings()),
    };

    await TestBed.configureTestingModule({
      imports: [MatchSettingsModalComponent],
      providers: [{ provide: GameService, useValue: mockGameService }],
    }).compileComponents();

    fixture = TestBed.createComponent(MatchSettingsModalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should render settings title with translation key', () => {
    const title = fixture.nativeElement.querySelector('.modal-title-group h2');
    expect(title.textContent).toContain('SETTINGS.TITLE');
  });

  it('should pre-fill draft from cached settings signal', () => {
    mockGameService.settings.set({ ...createDefaultGameSettings(), roundTimeSeconds: 90 });
    fixture.detectChanges();

    expect(component.draft().roundTimeSeconds).toBe(90);
  });

  it('should enable settings inputs for host', () => {
    const inputs = fixture.nativeElement.querySelectorAll('.config-field input, .config-field select');
    const disabled = Array.from(inputs as NodeListOf<HTMLInputElement | HTMLSelectElement>).filter(el => el.disabled);
    expect(disabled.length).toBe(0);
  });

  it('should render save and cancel buttons for host', () => {
    const saveBtn = fixture.nativeElement.querySelector('.modal-footer .btn-primary');
    const cancelBtn = fixture.nativeElement.querySelector('.modal-footer .btn-ghost');
    expect(saveBtn).toBeTruthy();
    expect(cancelBtn).toBeTruthy();
    expect(saveBtn.textContent).toContain('SETTINGS.SAVE');
    expect(cancelBtn.textContent).toContain('SETTINGS.CANCEL');
  });

  it('should disable settings inputs and hide save button for non-host', () => {
    mockGameService.meuConnectionId.set('conn2');
    mockGameService.players.set([
      { connectionId: 'conn1', name: 'Player1', isHost: true, team: '', isReady: false },
      { connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false },
    ]);
    fixture.detectChanges();

    const inputs = fixture.nativeElement.querySelectorAll('.config-field input, .config-field select');
    expect(inputs.length).toBeGreaterThan(0);
    const fields = Array.from(inputs as NodeListOf<HTMLInputElement | HTMLSelectElement>);
    const disabled = fields.filter(el => el.disabled);
    expect(disabled.length).toBe(fields.length);

    expect(fixture.nativeElement.querySelector('.modal-footer .btn-primary').textContent).toContain('SETTINGS.CLOSE');
    expect(fixture.nativeElement.querySelector('.modal-footer .btn-ghost')).toBeFalsy();
  });

  it('should show read-only note for non-host', () => {
    mockGameService.meuConnectionId.set('conn2');
    mockGameService.players.set([
      { connectionId: 'conn1', name: 'Player1', isHost: true, team: '', isReady: false },
      { connectionId: 'conn2', name: 'Player2', isHost: false, team: '', isReady: false },
    ]);
    fixture.detectChanges();

    const note = fixture.nativeElement.querySelector('.panel-note');
    expect(note).toBeTruthy();
    expect(note.textContent).toContain('SETTINGS.READ_ONLY');
  });

  it('should emit fechar when cancel button is clicked without saving', () => {
    const fecharSpy = vi.spyOn(component.fechar, 'emit');
    const cancelBtn = fixture.nativeElement.querySelector('.modal-footer .btn-ghost');
    cancelBtn.click();

    expect(fecharSpy).toHaveBeenCalled();
    expect(mockGameService.configurarPartida).not.toHaveBeenCalled();
  });

  it('should emit fechar when overlay is clicked', () => {
    const fecharSpy = vi.spyOn(component.fechar, 'emit');
    const overlay = fixture.nativeElement.querySelector('.modal-overlay');
    overlay.click();

    expect(fecharSpy).toHaveBeenCalled();
  });

  it('should save settings, show success feedback and emit fechar on success', async () => {
    const fecharSpy = vi.spyOn(component.fechar, 'emit');
    component.draft.set({ ...createDefaultGameSettings(), roundTimeSeconds: 90 });

    const saveBtn = fixture.nativeElement.querySelector('.modal-footer .btn-primary');
    saveBtn.click();
    await new Promise(resolve => setTimeout(resolve));

    expect(mockGameService.configurarPartida).toHaveBeenCalledWith(component.draft());
    expect(component.salvarFeedback()).toBe('SETTINGS.SALVO');
    expect(fecharSpy).not.toHaveBeenCalled();

    await new Promise(resolve => setTimeout(resolve, 750));
    expect(fecharSpy).toHaveBeenCalled();
  });

  it('should show error feedback and not emit fechar when save fails', async () => {
    const fecharSpy = vi.spyOn(component.fechar, 'emit');
    mockGameService.configurarPartida.mockResolvedValue(null);

    const saveBtn = fixture.nativeElement.querySelector('.modal-footer .btn-primary');
    saveBtn.click();
    await new Promise(resolve => setTimeout(resolve));
    fixture.detectChanges();

    expect(fecharSpy).not.toHaveBeenCalled();
    expect(component.salvarFeedback()).toBe('SETTINGS.ERRO');
    const feedback = fixture.nativeElement.querySelector('.salvar-feedback');
    expect(feedback).toBeTruthy();
    expect(feedback.classList).toContain('error');
  });

  it('should revert invalid number of rounds on save error', async () => {
    mockGameService.configurarPartida.mockResolvedValue(null);
    component.draft.set({ ...createDefaultGameSettings(), numberOfRounds: 5, roundTimeSeconds: 90 });

    await component.salvar();

    expect(component.draft().numberOfRounds).toBe(6);
    expect(component.draft().roundTimeSeconds).toBe(90);
  });

  it('should revert invalid tipoo lead limit on save error', async () => {
    mockGameService.configurarPartida.mockResolvedValue(null);
    component.draft.set({ ...createDefaultGameSettings(), tipooLeadLimit: 5 });

    await component.salvar();

    expect(component.draft().tipooLeadLimit).toBeNull();
  });

  it('should compute fill percent for slider', () => {
    expect(component.fillPercent(0, 120, 60)).toBe('50%');
    expect(component.fillPercent(30, 120, 30)).toBe('0%');
    expect(component.fillPercent(30, 120, 120)).toBe('100%');
    expect(component.fillPercent(30, 120, 999)).toBe('100%');
  });

  it('should format duration with minutes conversion', () => {
    expect(component.formatDuration(30)).toBe('30s');
    expect(component.formatDuration(60)).toBe('1min');
    expect(component.formatDuration(90)).toBe('1min 30s');
    expect(component.formatDuration(300)).toBe('5min');
    expect(component.formatDuration(600)).toBe('10min');
  });

  it('should render round time labels with selected value', () => {
    const labels = fixture.nativeElement.querySelectorAll('.slider-row span');
    expect(labels[0].textContent).toContain('SETTINGS.SELECTED');
    expect(labels[0].textContent).toContain('3min');
  });

  it('should always show rounds and tipoo fields', () => {
    const labels = Array.from(
      fixture.nativeElement.querySelectorAll('.config-label span') as NodeListOf<HTMLElement>,
    ).map(el => el.textContent?.trim());
    expect(labels).toContain('SETTINGS.NUMBER_OF_ROUNDS');
    expect(labels).toContain('SETTINGS.TIPOO_LEAD_LIMIT');
  });

  it('should show buzzer warning when all buzzers are deselected', () => {
    component.draft.set({ ...createDefaultGameSettings(), buzzerSounds: [] });
    fixture.detectChanges();

    const warning = fixture.nativeElement.querySelector('.config-warning');
    expect(warning).toBeTruthy();
    expect(warning.textContent).toContain('SETTINGS.BUZZER_WARNING');
  });

  it('should not show buzzer warning when at least one buzzer is selected', () => {
    expect(fixture.nativeElement.querySelector('.config-warning')).toBeFalsy();
  });

  it('should order difficulties as Fácil, Médio, Difícil', () => {
    const dificuldadeField = Array.from(
      fixture.nativeElement.querySelectorAll('.config-field') as NodeListOf<HTMLElement>,
    ).find(f => f.textContent?.includes('SETTINGS.DIFFICULTIES'));
    const items = Array.from(
      (dificuldadeField!.querySelectorAll('.checkbox-item span') as NodeListOf<HTMLElement>),
    ).map(el => el.textContent?.trim());
    expect(items).toEqual(['Fácil', 'Médio', 'Difícil']);
  });

  it('should not allow deselecting the last difficulty', () => {
    component.draft.set({ ...createDefaultGameSettings(), difficulties: ['Fácil'] });
    fixture.detectChanges();

    const dificuldadeField = Array.from(
      fixture.nativeElement.querySelectorAll('.config-field') as NodeListOf<HTMLElement>,
    ).find(f => f.textContent?.includes('SETTINGS.DIFFICULTIES'));
    const checkbox = dificuldadeField!.querySelector('.checkbox-item input') as HTMLInputElement;
    checkbox.click();

    expect(component.draft().difficulties).toEqual(['Fácil']);
    expect(checkbox.disabled).toBe(true);
  });

  it('should default starting team to aleatorio and render it selected', () => {
    const startingTeamSelect = Array.from(
      fixture.nativeElement.querySelectorAll('.config-field select') as NodeListOf<HTMLSelectElement>,
    ).find(s => Array.from(s.options).some(o => o.textContent?.includes('SETTINGS.TEAM')));
    expect(component.draft().startingTeam).toBe('aleatorio');
    expect(startingTeamSelect?.value).toBe('aleatorio');
    expect(startingTeamSelect?.options[startingTeamSelect!.selectedIndex].textContent).toContain(
      'SETTINGS.TEAM.aleatorio',
    );
  });

  it('should default round time to 3 minutes, rounds to 6 and tiebreak to empatado', () => {
    expect(component.draft().roundTimeSeconds).toBe(180);
    expect(component.draft().numberOfRounds).toBe(6);
    expect(component.draft().pauseBetweenRoundsSeconds).toBe(30);
    expect(component.draft().tiebreakMode).toBe('empatado');
  });

  it('should render tooltip buttons for every settings field', () => {
    const tooltips = fixture.nativeElement.querySelectorAll('app-tooltip .tooltip-btn');
    expect(tooltips.length).toBeGreaterThan(5);
  });

  it('should reject empty numeric fields, show required error and keep draft unchanged', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.draft().numberOfRounds).toBe(6);
    expect(component.erros().numberOfRounds).toBe('SETTINGS.ERRORS.REQUIRED');
    const error = fixture.nativeElement.querySelector('.field-error');
    expect(error).toBeTruthy();
    expect(error.textContent).toContain('SETTINGS.ERRORS.REQUIRED');
  });

  it('should reject out-of-range values without updating the draft', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '25';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.draft().numberOfRounds).toBe(6);
    expect(component.erros().numberOfRounds).toBe('SETTINGS.ERRORS.RANGE');
    expect(input.classList).toContain('field-invalid');
  });

  it('should reject odd number of rounds', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '5';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.draft().numberOfRounds).toBe(6);
    expect(component.erros().numberOfRounds).toBe('SETTINGS.ERRORS.EVEN');
  });

  it('should restore last valid value on blur after clearing the field', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(component.erros().numberOfRounds).toBe('SETTINGS.ERRORS.REQUIRED');

    input.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    expect(input.value).toBe('6');
    expect(component.erros().numberOfRounds).toBeUndefined();
  });

  it('should update the draft when a valid number is typed', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '8';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(component.draft().numberOfRounds).toBe(8);
    expect(component.erros().numberOfRounds).toBeUndefined();
    expect(fixture.nativeElement.querySelector('.field-error')).toBeFalsy();
  });

  it('should disable save button while a field has an error', () => {
    const input = fixture.nativeElement.querySelector('input[type="number"]');
    input.value = '25';
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const saveBtn = fixture.nativeElement.querySelector('.modal-footer .btn-primary') as HTMLButtonElement;
    expect(saveBtn.disabled).toBe(true);
  });

  it('should clear tipoo error when tipoo is toggled off', () => {
    component.draft.set({ ...createDefaultGameSettings(), tipooLeadLimit: 12 });
    fixture.detectChanges();

    const tipooInput = (Array.from(fixture.nativeElement.querySelectorAll('input[type="number"]')) as HTMLInputElement[]).find(
      el => el.value === '12',
    ) as HTMLInputElement;
    tipooInput.value = '';
    tipooInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(component.erros().tipooLeadLimit).toBe('SETTINGS.ERRORS.REQUIRED');

    const toggle = (Array.from(fixture.nativeElement.querySelectorAll('.inline-toggle input[type="checkbox"]')) as HTMLInputElement[]).find(
      el => (el.parentElement as HTMLElement).textContent?.includes('SETTINGS.TIPOO_ENABLED'),
    ) as HTMLInputElement;
    toggle.checked = false;
    toggle.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(component.draft().tipooLeadLimit).toBeNull();
    expect(component.erros().tipooLeadLimit).toBeUndefined();
  });
});
