import { Pipe, PipeTransform } from '@angular/core';
import { TranslateService } from '../services/translate.service';

@Pipe({
  name: 'translate',
  pure: false,
  standalone: true,
})
export class TranslatePipe implements PipeTransform {
  private cache = new Map<string, string>();
  private lastTranslations: Record<string, string> | null = null;

  constructor(private readonly translateService: TranslateService) {}

  transform(key: string | null | undefined): string {
    if (!key) {
      return '';
    }

    const current = this.translateService.translations();
    if (current !== this.lastTranslations) {
      this.lastTranslations = current;
      this.cache.clear();
    }

    if (!this.cache.has(key)) {
      this.cache.set(key, this.translateService.translate(key));
    }

    return this.cache.get(key)!;
  }
}
