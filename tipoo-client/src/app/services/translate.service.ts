import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { catchError, firstValueFrom, of } from 'rxjs';

export type Translations = Record<string, string>;

@Injectable({ providedIn: 'root' })
export class TranslateService {
  private readonly http = inject(HttpClient);

  readonly language = signal('pt-BR');
  readonly translations = signal<Translations>({});
  readonly loading = signal(false);
  readonly error = signal('');

  async use(lang: string): Promise<void> {
    if (!lang) {
      return;
    }

    this.language.set(lang);
    this.error.set('');
    this.loading.set(true);

    const path = `/assets/translate/${lang}.json`;

    try {
      const translations = await firstValueFrom(
        this.http.get<Translations>(path).pipe(catchError(() => of({})))
      );
      this.translations.set(translations ?? {});
    } catch (error) {
      this.translations.set({});
      this.error.set(`Erro ao carregar traduções para ${lang}`);
      console.error('[TranslateService] translation load error', error);
    } finally {
      this.loading.set(false);
    }
  }

  translate(key: string): string {
    if (!key) {
      return '';
    }

    const segments = key.split('.');
    let value: any = this.translations();

    for (const segment of segments) {
      if (value && typeof value === 'object' && segment in value) {
        value = value[segment];
      } else {
        return key;
      }
    }

    return typeof value === 'string' ? value : key;
  }

  instant(key: string): string {
    return this.translate(key);
  }
}
