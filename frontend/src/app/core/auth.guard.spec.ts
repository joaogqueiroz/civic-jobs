import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { roleGuard } from './auth.guard';
import { AuthService } from './auth.service';
import { UserRole } from './models';

describe('roleGuard', () => {
  let role: UserRole | null;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: {
            isAuthenticated: () => role !== null,
            hasRole: (wanted: UserRole) => role === wanted,
            home: () => (role === 'Candidate' ? '/open-positions' : '/department'),
          },
        },
      ],
    });
  });

  const runGuard = (wanted: UserRole, url: string) =>
    TestBed.runInInjectionContext(() =>
      roleGuard(wanted)({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );

  const serialize = (result: unknown) => TestBed.inject(Router).serializeUrl(result as UrlTree);

  it('lets a user with the role through', () => {
    role = 'Recruiter';

    expect(runGuard('Recruiter', '/job-postings')).toBe(true);
  });

  it('redirects an anonymous user to /login and remembers where they were going', () => {
    role = null;

    const result = runGuard('Recruiter', '/job-postings/new');

    expect(result).toBeInstanceOf(UrlTree);
    expect(serialize(result)).toBe('/login?returnUrl=%2Fjob-postings%2Fnew');
  });

  it('sends a user of another role to their own first page', () => {
    role = 'Candidate';

    const result = runGuard('Recruiter', '/job-postings');

    expect(serialize(result)).toBe('/open-positions');
  });
});
