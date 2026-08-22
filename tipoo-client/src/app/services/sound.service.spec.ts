import { TestBed } from '@angular/core/testing';
import { SoundService } from './sound.service';

describe('SoundService', () => {
  let service: SoundService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SoundService],
    });
    service = TestBed.inject(SoundService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should execute tocarBuzina without error', () => {
    expect(() => service.tocarBuzina('erro')).not.toThrow();
    expect(() => service.tocarBuzina('air_horn')).not.toThrow();
    expect(() => service.tocarBuzina('censura')).not.toThrow();
  });

  it('should execute tocarAcerto without error', () => {
    expect(() => service.tocarAcerto()).not.toThrow();
  });
});
