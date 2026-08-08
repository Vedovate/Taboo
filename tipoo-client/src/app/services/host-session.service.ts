import { Injectable } from '@angular/core';

const STORAGE_KEY = 'tipoo_host_session';

@Injectable({ providedIn: 'root' })
export class HostSessionService {
  getOrCreate(): string {
    const existing = this.load();
    if (existing) {
      return existing;
    }

    const token = this.generateId();
    this.save(token);
    return token;
  }

  private load(): string | null {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch {
      // localStorage pode não estar disponível (navegação anônima, etc.)
      return null;
    }
  }

  private save(token: string): void {
    try {
      localStorage.setItem(STORAGE_KEY, token);
    } catch {
      // localStorage pode não estar disponível
    }
  }

  private generateId(): string {
    const cryptoObj = globalThis.crypto;
    if (cryptoObj && typeof cryptoObj.randomUUID === 'function') {
      return cryptoObj.randomUUID();
    }
    return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
  }
}
