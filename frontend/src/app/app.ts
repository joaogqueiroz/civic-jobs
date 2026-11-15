import { Component, computed, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatToolbarModule } from '@angular/material/toolbar';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';
import { UserRole } from './core/models';
import { ROLE_LABELS } from './shared/role-labels';

interface NavLink {
  path: string;
  label: string;
}

/** The pages each role works in; shown in the toolbar. */
const NAV_BY_ROLE: Record<UserRole, readonly NavLink[]> = {
  Admin: [{ path: '/admin', label: 'Agency' }],
  Recruiter: [{ path: '/job-postings', label: 'Job postings' }],
  HiringManager: [{ path: '/department', label: 'Department' }],
  Candidate: [
    { path: '/open-positions', label: 'Open positions' },
    { path: '/my-applications', label: 'My applications' },
  ],
};

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, MatToolbarModule, MatButtonModule, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly auth = inject(AuthService);

  protected readonly links = computed(() => {
    const role = this.auth.role();
    return role ? NAV_BY_ROLE[role] : [];
  });

  protected readonly roleLabel = computed(() => {
    const user = this.auth.currentUser();
    if (!user) {
      return '';
    }
    return user.departmentName ? `${ROLE_LABELS[user.role]} · ${user.departmentName}` : ROLE_LABELS[user.role];
  });
}
