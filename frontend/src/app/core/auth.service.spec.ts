import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthService } from './auth.service';
import { User } from './models';

describe('AuthService', () => {
  const recruiter: User = {
    id: '1',
    email: 'recruiter@cityofexample.gov',
    fullName: 'Jordan Lee',
    role: 'Recruiter',
    agencyId: 'a1',
    agencyName: 'City of Example',
    departmentId: 'd1',
    departmentName: 'Parks & Recreation',
    createdAt: '2026-11-08T12:00:00Z',
  };

  let service: AuthService;
  let http: HttpTestingController;

  const inOneHour = () => new Date(Date.now() + 60 * 60 * 1000).toISOString();
  const anHourAgo = () => new Date(Date.now() - 60 * 60 * 1000).toISOString();

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('login posts the credentials, then loads the user so the app knows the role', () => {
    let loggedIn: User | undefined;

    service.login({ email: recruiter.email, password: 'Demo@12345' }).subscribe((user) => (loggedIn = user));

    const login = http.expectOne('/identity/api/auth/login');
    expect(login.request.method).toBe('POST');
    expect(login.request.body).toEqual({ email: recruiter.email, password: 'Demo@12345' });
    login.flush({ accessToken: 'set-as-a-cookie', expiresAt: '2026-11-08T13:00:00Z' });
    http.expectOne('/identity/api/users/me').flush(recruiter);

    expect(loggedIn?.role).toBe('Recruiter');
    expect(service.isAuthenticated()).toBe(true);
    expect(service.home()).toBe('/job-postings');
  });

  it('never keeps the token in localStorage: the session is an httpOnly cookie', () => {
    service.login({ email: recruiter.email, password: 'Demo@12345' }).subscribe();
    http.expectOne('/identity/api/auth/login').flush({ accessToken: 'secret-token', expiresAt: inOneHour() });
    http.expectOne('/identity/api/users/me').flush(recruiter);

    const stored = Object.keys(localStorage).map((key) => localStorage.getItem(key) ?? '');
    expect(stored.some((value) => value.includes('secret-token'))).toBe(false);
  });

  it('register posts the new candidate and does not log in', () => {
    const input = { fullName: 'Alex Kim', email: 'alex@example.com', password: 'Demo@12345' };

    service.register(input).subscribe();

    const request = http.expectOne('/identity/api/auth/register');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(input);
    request.flush({ ...recruiter, role: 'Candidate' });
    expect(service.isAuthenticated()).toBe(false);
  });

  it('restoreSession logs the user back in when the session cookie is still valid', () => {
    localStorage.setItem('civicjobs.sessionExpiresAt', inOneHour());

    service.restoreSession().subscribe();

    http.expectOne('/identity/api/users/me').flush(recruiter);

    expect(service.currentUser()?.fullName).toBe('Jordan Lee');
  });

  it('restoreSession does not call the API when no session was started, so anonymous visits log no 401', () => {
    let completed = false;

    service.restoreSession().subscribe({ complete: () => (completed = true) });

    http.expectNone('/identity/api/users/me');
    expect(completed).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
  });

  it('restoreSession does not call the API when the session has expired', () => {
    localStorage.setItem('civicjobs.sessionExpiresAt', anHourAgo());

    service.restoreSession().subscribe();

    http.expectNone('/identity/api/users/me');
  });

  it('restoreSession completes without a user when the API no longer accepts the session', () => {
    localStorage.setItem('civicjobs.sessionExpiresAt', inOneHour());
    let completed = false;

    service.restoreSession().subscribe({ complete: () => (completed = true) });
    http.expectOne('/identity/api/users/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(completed).toBe(true);
    expect(service.isAuthenticated()).toBe(false);
    expect(service.home()).toBe('/open-positions');
  });

  it('loadCurrentUser sends one request when called again while the first is still running', () => {
    service.loadCurrentUser().subscribe();
    service.loadCurrentUser().subscribe();

    http.expectOne('/identity/api/users/me').flush(recruiter);

    expect(service.currentUser()?.fullName).toBe('Jordan Lee');
  });

  it('home is the first page of the role', () => {
    service.loadCurrentUser().subscribe();
    http.expectOne('/identity/api/users/me').flush({ ...recruiter, role: 'HiringManager' });

    expect(service.home()).toBe('/department');
  });

  it('logout asks the API to expire the cookie, forgets the user and navigates to /login', () => {
    const navigate = vi.spyOn(TestBed.inject(Router), 'navigateByUrl').mockResolvedValue(true);
    service.loadCurrentUser().subscribe();
    http.expectOne('/identity/api/users/me').flush(recruiter);

    service.logout();
    const request = http.expectOne('/identity/api/auth/logout');
    expect(request.request.method).toBe('POST');
    request.flush(null, { status: 204, statusText: 'No Content' });

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('civicjobs.sessionExpiresAt')).toBeNull();
    expect(navigate).toHaveBeenCalledWith('/login');
  });
});
