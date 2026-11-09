import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { TokenStorage } from './token-storage';

describe('AuthService', () => {
  const inOneHour = () => new Date(Date.now() + 60 * 60 * 1000).toISOString();
  const anHourAgo = () => new Date(Date.now() - 60 * 60 * 1000).toISOString();

  let service: AuthService;
  let http: HttpTestingController;
  let storage: TokenStorage;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    storage = TestBed.inject(TokenStorage);
  });

  afterEach(() => http.verify());

  it('login posts the credentials and stores the token', () => {
    service.login({ email: 'recruiter@cityofexample.gov', password: 'Demo@12345' }).subscribe();

    const request = http.expectOne('/identity/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'recruiter@cityofexample.gov', password: 'Demo@12345' });
    request.flush({ accessToken: 'jwt-token', expiresAt: inOneHour() });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken()).toBe('jwt-token');
    expect(storage.get()?.accessToken).toBe('jwt-token');
  });

  it('register posts the new user and does not log in', () => {
    const input = { fullName: 'Jane Doe', email: 'jane@cityofexample.gov', password: 'Demo@12345' };

    service.register(input).subscribe();

    const request = http.expectOne('/identity/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(input);
    request.flush({ id: '1', email: input.email, fullName: input.fullName, createdAt: '2026-11-08T12:00:00Z' });
    expect(service.isAuthenticated()).toBe(false);
  });

  it('loadCurrentUser stores the user returned by /users/me', () => {
    service.loadCurrentUser().subscribe();

    http
      .expectOne('/identity/api/users/me')
      .flush({ id: '1', email: 'jane@cityofexample.gov', fullName: 'Jane Doe', createdAt: '2026-11-08T12:00:00Z' });

    expect(service.currentUser()?.fullName).toBe('Jane Doe');
  });

  it('is not authenticated when the stored token has expired', () => {
    storage.set({ accessToken: 'old-token', expiresAt: anHourAgo() });

    const restored = TestBed.runInInjectionContext(() => new AuthService());

    expect(restored.isAuthenticated()).toBe(false);
    expect(restored.accessToken()).toBeNull();
  });

  it('restores a valid token saved by a previous session', () => {
    storage.set({ accessToken: 'saved-token', expiresAt: inOneHour() });

    const restored = TestBed.runInInjectionContext(() => new AuthService());

    expect(restored.isAuthenticated()).toBe(true);
    expect(restored.accessToken()).toBe('saved-token');
  });

  it('logout clears the token and the user and navigates to /login', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    service.login({ email: 'jane@cityofexample.gov', password: 'Demo@12345' }).subscribe();
    http.expectOne('/identity/api/auth/login').flush({ accessToken: 'jwt-token', expiresAt: inOneHour() });

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(service.currentUser()).toBeNull();
    expect(storage.get()).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
