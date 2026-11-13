import { HttpClient } from '@angular/common/http';
import { computed, inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, map, of, shareReplay, switchMap, tap } from 'rxjs';
import { AuthToken, LoginInput, RegisterInput, User, UserRole } from './models';

const API = '/identity/api';

/**
 * When the session cookie expires. Not a secret (the token stays in the httpOnly cookie): it only tells the app
 * whether asking /users/me at startup makes sense, so anonymous visits don't log a 401 in the console.
 */
const SESSION_EXPIRES_KEY = 'civicjobs.sessionExpiresAt';

/** The first page of each role after login (docs/user-story.md, Frontend). */
export const HOME_BY_ROLE: Record<UserRole, string> = {
  Admin: '/admin',
  Recruiter: '/job-postings',
  HiringManager: '/department',
  Candidate: '/open-positions',
};

/**
 * The session lives in an httpOnly cookie set by the Identity API (BR-21), so this service never sees the token.
 * It knows who is logged in from /users/me: at startup, after login, and until logout or a 401.
 */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);

  private readonly user = signal<User | null>(null);
  private pendingUser?: Observable<User>;

  readonly currentUser = this.user.asReadonly();
  readonly role = computed(() => this.user()?.role ?? null);
  readonly home = computed(() => {
    const role = this.role();
    return role ? HOME_BY_ROLE[role] : '/open-positions';
  });

  isAuthenticated(): boolean {
    return this.user() !== null;
  }

  hasRole(role: UserRole): boolean {
    return this.role() === role;
  }

  /** Called once at startup: a valid session cookie from a previous visit logs the user back in. */
  restoreSession(): Observable<void> {
    if (!this.sessionMayExist()) {
      return of(undefined);
    }

    return this.loadCurrentUser().pipe(
      map(() => undefined),
      catchError(() => of(undefined)),
    );
  }

  /** Logs in (the API sets the session cookie) and returns the user, so the caller can route by role. */
  login(input: LoginInput): Observable<User> {
    return this.http.post<AuthToken>(`${API}/auth/login`, input).pipe(
      tap((token) => this.writeSessionExpiry(token.expiresAt)),
      switchMap(() => this.loadCurrentUser()),
    );
  }

  register(input: RegisterInput): Observable<User> {
    return this.http.post<User>(`${API}/auth/register`, input);
  }

  /**
   * Loads the current user. Callers that ask while a request is still running share it, so only one
   * /users/me request is sent when several parts of the app ask at the same moment.
   */
  loadCurrentUser(): Observable<User> {
    this.pendingUser ??= this.http.get<User>(`${API}/users/me`).pipe(
      tap((user) => this.user.set(user)),
      finalize(() => (this.pendingUser = undefined)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.pendingUser;
  }

  /** Asks the API to expire the cookie, then forgets the user and goes to the login page. */
  logout(): void {
    this.http
      .post(`${API}/auth/logout`, null)
      .pipe(catchError(() => of(null)))
      .subscribe(() => this.endSession());
  }

  /** Forgets the user without calling the API, for when the API already said the session is over (401). */
  endSession(): void {
    this.writeSessionExpiry(null);
    this.user.set(null);
    void this.router.navigateByUrl('/login');
  }

  private sessionMayExist(): boolean {
    try {
      const expiresAt = localStorage.getItem(SESSION_EXPIRES_KEY);
      return expiresAt !== null && Date.parse(expiresAt) > Date.now();
    } catch {
      return false;
    }
  }

  private writeSessionExpiry(expiresAt: string | null): void {
    try {
      if (expiresAt) {
        localStorage.setItem(SESSION_EXPIRES_KEY, expiresAt);
      } else {
        localStorage.removeItem(SESSION_EXPIRES_KEY);
      }
    } catch {
      // Storage can be blocked (private mode); the app then just asks the user to log in again after a reload.
    }
  }
}
