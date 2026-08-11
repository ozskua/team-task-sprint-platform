import { Injectable } from '@angular/core';

import { AuthSession } from '../models/auth-models';

/**
 * The only class that touches browser storage, so moving the session elsewhere is a one-file change.
 */
@Injectable()
export class TokenStorage {
  private static readonly StorageKey = 'sprintflow.session';

  // localStorage persists the session across refreshes, but any script on the page can read it.
  // Accepted because the token is short-lived and the server re-validates it on every request.

  /** Anything unreadable is treated as "not signed in" and cleared, so it cannot break start-up. */
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
      // Storage can be unavailable (private mode, quota, disabled). The user stays signed in for
      // this tab; only persistence across a reload is lost.
    }
  }

  clear(): void {
    try {
      localStorage.removeItem(TokenStorage.StorageKey);
    } catch {
      // Nothing useful to do if removal fails.
    }
  }

  /** Parsed JSON is `unknown`; this is what makes it safe to hand back as an `AuthSession`. */
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
