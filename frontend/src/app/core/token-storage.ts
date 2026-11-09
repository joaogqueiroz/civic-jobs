import { Injectable } from '@angular/core';
import { AuthToken } from './models';

const STORAGE_KEY = 'civicjobs.token';

/**
 * Keeps the access token in localStorage for the demo (PRD section 10).
 * Production would use an httpOnly cookie instead.
 */
@Injectable({ providedIn: 'root' })
export class TokenStorage {
  get(): AuthToken | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as AuthToken) : null;
    } catch {
      return null;
    }
  }

  set(token: AuthToken): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(token));
  }

  clear(): void {
    localStorage.removeItem(STORAGE_KEY);
  }
}
