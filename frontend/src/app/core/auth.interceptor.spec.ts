import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let authenticated: boolean;
  const endSession = vi.fn();

  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    authenticated = true;
    endSession.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { isAuthenticated: () => authenticated, endSession } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('adds no Authorization header: the browser sends the session cookie', () => {
    http.get('/jobs/api/job-postings').subscribe();

    const request = controller.expectOne('/jobs/api/job-postings');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('ends the session when the API answers 401', () => {
    http.get('/jobs/api/job-postings').subscribe({ error: () => undefined });

    controller.expectOne('/jobs/api/job-postings').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(endSession).toHaveBeenCalledOnce();
  });

  it('does not end the session when login itself answers 401 (wrong password)', () => {
    authenticated = false;

    http.post('/identity/api/auth/login', {}).subscribe({ error: () => undefined });

    controller.expectOne('/identity/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(endSession).not.toHaveBeenCalled();
  });

  it('does not end the session when the startup check finds no session', () => {
    authenticated = false;

    http.get('/identity/api/users/me').subscribe({ error: () => undefined });

    controller.expectOne('/identity/api/users/me').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(endSession).not.toHaveBeenCalled();
  });

  it('passes the error on to the caller', () => {
    let status = 0;
    http.get('/jobs/api/job-postings/1').subscribe({ error: (error) => (status = error.status) });

    controller.expectOne('/jobs/api/job-postings/1').flush(null, { status: 404, statusText: 'Not Found' });

    expect(status).toBe(404);
    expect(endSession).not.toHaveBeenCalled();
  });
});
