import { HttpClient } from '@angular/common/http';
import { inject, Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, finalize, map, shareReplay, tap } from 'rxjs';
import { AuthToken, LoginInput, RegisterInput, User } from './models';
import { TokenStorage } from './token-storage';

const API = '/identity/api';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly storage = inject(TokenStorage);

  private readonly token = signal<AuthToken | null>(this.storage.get());
  private readonly user = signal<User | null>(null);
  private pendingUser?: Observable<User>;

  readonly currentUser = this.user.asReadonly();

  isAuthenticated(): boolean {
    return this.accessToken() !== null;
  }

  /** The stored token, or null when there is none or it has expired. */
  accessToken(): string | null {
    const token = this.token();
    return token && Date.parse(token.expiresAt) > Date.now() ? token.accessToken : null;
  }

  login(input: LoginInput): Observable<void> {
    return this.http.post<AuthToken>(`${API}/auth/login`, input).pipe(
      tap((token) => {
        this.storage.set(token);
        this.token.set(token);
      }),
      map(() => undefined),
    );
  }

  register(input: RegisterInput): Observable<User> {
    return this.http.post<User>(`${API}/auth/register`, input);
  }

  /**
   * Loads the current user. Callers that ask while a request is still running share it, so right after
   * login (when the login page and the app shell both ask) only one /users/me request is sent.
   */
  loadCurrentUser(): Observable<User> {
    this.pendingUser ??= this.http.get<User>(`${API}/users/me`).pipe(
      tap((user) => this.user.set(user)),
      finalize(() => (this.pendingUser = undefined)),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    return this.pendingUser;
  }

  logout(): void {
    this.storage.clear();
    this.token.set(null);
    this.user.set(null);
    void this.router.navigateByUrl('/login');
  }
}
