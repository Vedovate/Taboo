import { Injectable } from '@angular/core';

const STORAGE_KEY = 'tipoo_player_name';

@Injectable({ providedIn: 'root' })
export class PlayerStorageService {
  saveName(name: string): void {
    try {
      localStorage.setItem(STORAGE_KEY, name);
    } catch {
      // localStorage pode não estar disponível (navegação anônima, etc.)
    }
  }

  loadName(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      return null;
    }
  }
}
