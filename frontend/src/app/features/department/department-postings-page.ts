import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { JobPostingStatus } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { STATUS_LABELS, StatusBadge } from '../../shared/status-badge';
import { DepartmentService } from './department.service';

/** Statuses the hiring manager sees: drafts stay with the recruiter until they are submitted (BR-16). */
const FILTERS: readonly JobPostingStatus[] = ['PendingApproval', 'Open', 'Closed'];

/** The hiring manager's list of department postings, waiting for approval first (FR-15). */
@Component({
  selector: 'app-department-postings-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatChipsModule, MatProgressBarModule, StatusBadge],
  templateUrl: './department-postings-page.html',
  styleUrl: './department-postings-page.scss',
})
export class DepartmentPostingsPage {
  private readonly service = inject(DepartmentService);
  private readonly notifier = inject(Notifier);
  private readonly router = inject(Router);
  protected readonly auth = inject(AuthService);

  /** Status filter from the URL (?status=Open). */
  readonly status = input<string>();

  protected readonly selectedStatus = computed(() => FILTERS.find((status) => status === this.status()));

  protected readonly postings = rxResource({ stream: () => this.service.list() });

  protected readonly visible = computed(() => {
    const all = this.postings.hasValue() ? this.postings.value() : [];
    const status = this.selectedStatus();
    return status ? all.filter((posting) => posting.status === status) : all;
  });

  protected readonly filters = computed(() => {
    const all = this.postings.hasValue() ? this.postings.value() : [];
    return [
      { value: '', label: 'All', count: all.length },
      ...FILTERS.map((status) => ({
        value: status,
        label: STATUS_LABELS[status],
        count: all.filter((posting) => posting.status === status).length,
      })),
    ];
  });

  constructor() {
    effect(() => {
      const error = this.postings.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }

  protected filterBy(status: string | null | undefined): void {
    void this.router.navigate([], { queryParams: { status: status || null } });
  }
}
