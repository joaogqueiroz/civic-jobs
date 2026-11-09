import { BreakpointObserver } from '@angular/cdk/layout';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { JobPostingStatus } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { StatusBadge } from '../../shared/status-badge';
import { JobPostingsService } from './job-postings.service';
import { STATUSES } from './labels';

@Component({
  selector: 'app-job-postings-list-page',
  imports: [
    RouterLink,
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatTableModule,
    StatusBadge,
  ],
  templateUrl: './job-postings-list-page.html',
  styleUrl: './job-postings-list-page.scss',
})
export class JobPostingsListPage {
  private readonly service = inject(JobPostingsService);
  protected readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  /** Status filter, kept in the URL (?status=Open) so it survives navigation back from a posting. */
  readonly status = input<string>();

  protected readonly statuses = STATUSES;
  protected readonly columns = ['title', 'department', 'status', 'closingDate', 'salary'];

  protected readonly selectedStatus = computed(() =>
    STATUSES.find((status) => status === this.status()),
  );

  protected readonly isMobile = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 768px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  protected readonly postings = rxResource({
    params: () => ({ status: this.selectedStatus() }),
    stream: ({ params }) => this.service.list(params.status),
  });

  constructor() {
    effect(() => {
      if (this.postings.status() === 'error') {
        this.notifier.error(this.postings.error());
      }
    });
  }

  protected filterBy(status: JobPostingStatus | '' | null): void {
    void this.router.navigate([], { queryParams: { status: status || null } });
  }
}
