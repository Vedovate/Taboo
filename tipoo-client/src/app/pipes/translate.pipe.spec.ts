import { signal, WritableSignal } from '@angular/core';
import { TranslatePipe } from './translate.pipe';
import { TranslateService } from '../services/translate.service';

interface MockTranslateService {
  language: WritableSignal<string>;
  translations: WritableSignal<Record<string, string>>;
  loading: WritableSignal<boolean>;
  error: WritableSignal<string>;
  use: ReturnType<typeof vi.fn>;
  translate: ReturnType<typeof vi.fn>;
  instant: ReturnType<typeof vi.fn>;
}

describe('TranslatePipe', () => {
  let pipe: TranslatePipe;
  let mockService: MockTranslateService;

  beforeEach(() => {
    mockService = {
      language: signal('pt-BR'),
      translations: signal({
        'HOME.WELCOME_TITLE': 'Bem-vindo ao Taboo',
        'HOME.TAGLINE': 'Jogo de palavras',
      }),
      loading: signal(false),
      error: signal(''),
      use: vi.fn().mockResolvedValue(undefined),
      translate: vi.fn((key: string) => {
        const t = mockService.translations();
        return t[key] ?? key;
      }),
      instant: vi.fn((key: string) => key),
    };

    pipe = new TranslatePipe(mockService as unknown as TranslateService);
  });

  describe('transform', () => {
    it('should return empty string for null', () => {
      expect(pipe.transform(null)).toBe('');
    });

    it('should return empty string for undefined', () => {
      expect(pipe.transform(undefined)).toBe('');
    });

    it('should translate a known key', () => {
      const result = pipe.transform('HOME.WELCOME_TITLE');
      expect(result).toBe('Bem-vindo ao Taboo');
    });

    it('should return the key if translation is not found', () => {
      const result = pipe.transform('UNKNOWN.KEY');
      expect(result).toBe('UNKNOWN.KEY');
    });

    it('should cache repeated calls and reuse cached value', () => {
      const spy = vi.spyOn(mockService, 'translate');

      pipe.transform('HOME.WELCOME_TITLE');
      pipe.transform('HOME.WELCOME_TITLE');

      expect(spy).toHaveBeenCalledTimes(1);
    });

    it('should call translate again for different keys', () => {
      const spy = vi.spyOn(mockService, 'translate');

      pipe.transform('HOME.WELCOME_TITLE');
      pipe.transform('HOME.TAGLINE');

      expect(spy).toHaveBeenCalledTimes(2);
    });
  });

  describe('cache invalidation on language change', () => {
    it('should clear cache when translations signal changes', () => {
      pipe.transform('HOME.WELCOME_TITLE');

      mockService.translations.set({ 'HOME.WELCOME_TITLE': 'Welcome to Taboo' });

      const spy = vi.spyOn(mockService, 'translate');
      const result = pipe.transform('HOME.WELCOME_TITLE');

      expect(spy).toHaveBeenCalled();
      expect(result).toBe('Welcome to Taboo');
    });
  });
});
