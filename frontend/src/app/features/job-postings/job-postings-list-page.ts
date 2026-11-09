import { BreakpointObserver } from '@angular/cdk/layout';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input } from '@angular/core';
import { rxResource, toSignal } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';
import { map, of } from 'rxjs';
import { JobPosting, JobPostingStatus } from '../../core/models';
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
    MatChipsModule,
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
  private readonly notifier = inject(Notifier);
  protected readonly router = inject(Router);

  /** Status filter, kept in the URL (?status=Open) so it survives navigation back from a posting. */
  readonly status = input<string>();

  protected readonly columns = ['title', 'status', 'closingDate', 'salary'];

  protected readonly selectedStatus = computed(() =>
    STATUSES.find((status) => status === this.status()),
  );

  protected readonly isMobile = toSignal(
    inject(BreakpointObserver)
      .observe('(max-width: 768px)')
      .pipe(map((state) => state.matches)),
    { initialValue: false },
  );

  /** All of the recruiter's postings: the source of the counts on the filter chips. */
  private readonly all = rxResource({ stream: () => this.service.list() });

  /** The filtered list, from the API's ?status= filter (not requested when "All" is selected). */
  private readonly filtered = rxResource({
    params: () => ({ status: this.selectedStatus() }),
    stream: ({ params }) => (params.status ? this.service.list(params.status) : of(null)),
  });

  protected readonly isLoading = computed(() => this.all.isLoading() || this.filtered.isLoading());

  protected readonly postings = computed<JobPosting[] | undefined>(() => {
    const filtered = this.filtered.hasValue() ? this.filtered.value() : null;
    if (this.selectedStatus()) {
      return filtered ?? undefined;
    }
    return this.all.hasValue() ? this.all.value() : undefined;
  });

  protected readonly filters = computed(() => {
    const all = this.all.hasValue() ? this.all.value() : [];
    return [
      { value: '', label: 'All', count: all.length },
      ...STATUSES.map((status) => ({
        value: status,
        label: status,
        count: all.filter((posting) => posting.status === status).length,
      })),
    ];
  });

  constructor() {
    effect(() => {
      const error = this.all.error() ?? this.filtered.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }

  protected filterBy(status: JobPostingStatus | '' | null | undefined): void {
    void this.router.navigate([], { queryParams: { status: status || null } });
  }

  protected open(posting: JobPosting): void {
    void this.router.navigate(['/job-postings', posting.id]);
  }
}
