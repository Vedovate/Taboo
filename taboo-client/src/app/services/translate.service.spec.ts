import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient, withInterceptorsFromDi } from '@angular/common/http';
import { TranslateService } from './translate.service';

describe('TranslateService', () => {
  let service: TranslateService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptorsFromDi()),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(TranslateService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('use', () => {
    it('should load translations and update signals', async () => {
      const mockTranslations = { greeting: 'Olá', farewell: 'Tchau' };

      const promise = service.use('pt-BR');
      const req = httpMock.expectOne('/assets/translate/pt-BR.json');
      expect(req.request.method).toBe('GET');
      req.flush(mockTranslations);

      await promise;

      expect(service.language()).toBe('pt-BR');
      expect(service.loading()).toBe(false);
      expect(service.translations()).toEqual(mockTranslations);
      expect(service.error()).toBe('');
    });

    it('should handle empty lang gracefully', async () => {
      await service.use('');

      expect(service.loading()).toBe(false);
    });

    it('should handle HTTP error via catchError', async () => {
      const promise = service.use('en-US');
      const req = httpMock.expectOne('/assets/translate/en-US.json');
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });

      await promise;

      expect(service.translations()).toEqual({});
      expect(service.loading()).toBe(false);
    });

    it('should set loading true then false after completion', async () => {
      const promise = service.use('pt-BR');
      expect(service.loading()).toBe(true);

      const req = httpMock.expectOne('/assets/translate/pt-BR.json');
      req.flush({ key: 'value' });
      await promise;

      expect(service.loading()).toBe(false);
    });
  });

  describe('translate', () => {
    it('should resolve a simple key', () => {
      service.translations.set({ greeting: 'Olá' });

      const result = service.translate('greeting');

      expect(result).toBe('Olá');
    });

    it('should resolve a nested key', () => {
      service.translations.set({ HOME: { TITLE: 'Bem-vindo' } } as any);

      const result = service.translate('HOME.TITLE');

      expect(result).toBe('Bem-vindo');
    });

    it('should return the key when translation is not found', () => {
      service.translations.set({ existing: 'value' });

      const result = service.translate('non.existent');

      expect(result).toBe('non.existent');
    });

    it('should return empty string for falsy key', () => {
      const result = service.translate('');

      expect(result).toBe('');
    });

    it('should return the key when translation is not a string', () => {
      service.translations.set({ nested: { obj: 'value' } } as any);

      const result = service.translate('nested');

      expect(result).toBe('nested');
    });

    it('should handle deeply nested keys', () => {
      service.translations.set({
        A: { B: { C: { D: 'deep value' } } },
      } as any);

      const result = service.translate('A.B.C.D');

      expect(result).toBe('deep value');
    });

    it('should return key when intermediate path is missing', () => {
      service.translations.set({ A: { B: 'value' } } as any);

      const result = service.translate('A.X.Y');

      expect(result).toBe('A.X.Y');
    });
  });

  describe('instant', () => {
    it('should be an alias for translate', () => {
      service.translations.set({ hello: 'world' });

      expect(service.instant('hello')).toBe(service.translate('hello'));
    });
  });
});
