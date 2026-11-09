import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { authInterceptor } from './auth.interceptor';
import { AuthService } from './auth.service';

describe('authInterceptor', () => {
  let token: string | null;
  const logout = vi.fn();

  let http: HttpClient;
  let controller: HttpTestingController;

  beforeEach(() => {
    token = 'jwt-token';
    logout.mockClear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: { accessToken: () => token, logout } },
      ],
    });
    http = TestBed.inject(HttpClient);
    controller = TestBed.inject(HttpTestingController);
  });

  afterEach(() => controller.verify());

  it('adds the Bearer token to API requests', () => {
    http.get('/jobs/api/job-postings').subscribe();

    const request = controller.expectOne('/jobs/api/job-postings');
    expect(request.request.headers.get('Authorization')).toBe('Bearer jwt-token');
    request.flush([]);
  });

  it('sends no Authorization header when there is no token', () => {
    token = null;

    http.get('/jobs/api/job-postings').subscribe();

    const request = controller.expectOne('/jobs/api/job-postings');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('logs out when the API answers 401', () => {
    http.get('/jobs/api/job-postings').subscribe({ error: () => undefined });

    controller.expectOne('/jobs/api/job-postings').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).toHaveBeenCalledOnce();
  });

  it('does not log out when login itself answers 401 (wrong password)', () => {
    token = null;

    http.post('/identity/api/auth/login', {}).subscribe({ error: () => undefined });

    controller.expectOne('/identity/api/auth/login').flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(logout).not.toHaveBeenCalled();
  });

  it('passes the error on to the caller', () => {
    let status = 0;
    http.get('/jobs/api/job-postings/1').subscribe({ error: (error) => (status = error.status) });

    controller.expectOne('/jobs/api/job-postings/1').flush(null, { status: 404, statusText: 'Not Found' });

    expect(status).toBe(404);
    expect(logout).not.toHaveBeenCalled();
  });
});
