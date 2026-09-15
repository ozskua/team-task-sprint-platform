import { Injectable } from '@angular/core';

import { AuthSession } from '../models/auth-models';

@Injectable()
export class TokenStorage {
  private static readonly StorageKey = 'sprintflow.session';

read(): AuthSession | null {
    try {
      const raw = localStorage.getItem(TokenStorage.StorageKey);

      if (raw === null) {
        return null;
      }

      const parsed: unknown = JSON.parse(raw);

      if (!TokenStorage.isAuthSession(parsed)) {
        this.clear();
        return null;
      }

      return parsed;
    } catch {
      this.clear();
      return null;
    }
  }

  save(session: AuthSession): void {
    try {
      localStorage.setItem(TokenStorage.StorageKey, JSON.stringify(session));
    } catch {
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(TokenStorage.StorageKey);
    } catch {
    }
  }

  private static isAuthSession(value: unknown): value is AuthSession {
    if (typeof value !== 'object' || value === null) {
      return false;
    }

    const candidate = value as Record<string, unknown>;

    return typeof candidate['token'] === 'string'
      && candidate['token'].length > 0
      && typeof candidate['fullName'] === 'string'
      && typeof candidate['isAdmin'] === 'boolean';
  }
}
