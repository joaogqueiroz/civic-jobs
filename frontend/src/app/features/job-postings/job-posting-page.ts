import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { Observable, defer, filter, finalize, switchMap } from 'rxjs';
import { applyServerErrors } from '../../core/api-errors';
import { AuthService } from '../../core/auth.service';
import { EmploymentType, JobPosting, JobPostingInput, JobPostingStatus } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { formFieldDefaults } from '../../shared/form-field-defaults';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog';
import { StatusBadge } from '../../shared/status-badge';
import { JobPostingsService } from './job-postings.service';
import { EMPLOYMENT_TYPES, STATUSES } from './labels';

export type JobPostingPageMode = 'create' | 'view' | 'edit';

/**
 * One page for a posting: create (/new) and detail/edit (/:id and /:id/edit share the same layout).
 * Form on the left, status panel with the life cycle and its actions on the right.
 */
@Component({
  selector: 'app-job-posting-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    DatePipe,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    StatusBadge,
  ],
  providers: [formFieldDefaults],
  templateUrl: './job-posting-page.html',
  styleUrl: './job-posting-page.scss',
})
export class JobPostingPage {
  private readonly service = inject(JobPostingsService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly auth = inject(AuthService);

  /** From the route data. */
  readonly mode = input.required<JobPostingPageMode>();
  /** From the route parameter (absent when creating). */
  readonly id = input<string>();

  protected readonly steps = STATUSES;
  /** Short names that fit the four-step track in the side panel. */
  protected readonly stepLabels: Record<JobPostingStatus, string> = {
    Draft: 'Draft',
    PendingApproval: 'Approval',
    Open: 'Open',
    Closed: 'Closed',
  };
  protected readonly employmentTypes = EMPLOYMENT_TYPES;
  protected readonly today = new Date().toLocaleDateString('en-CA'); // yyyy-MM-dd, local date
  protected readonly busy = signal(false);

  protected readonly posting = rxResource({
    params: () => (this.mode() === 'create' ? undefined : this.id()),
    stream: ({ params: id }) => this.service.get(id),
  });

  protected readonly current = computed(() => (this.posting.hasValue() ? this.posting.value() : undefined));
  protected readonly isCreate = computed(() => this.mode() === 'create');
  /** Closed postings are a record, and postings waiting for approval are frozen until the decision (BR-06). */
  protected readonly isReadOnly = computed(() => {
    const status = this.current()?.status;
    return status === 'Closed' || status === 'PendingApproval';
  });
  /** The department comes from the recruiter's account (BR-13); an existing posting keeps the one it was created in. */
  protected readonly department = computed(
    () => this.current()?.department ?? this.auth.currentUser()?.departmentName ?? '',
  );

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    location: ['', [Validators.required, Validators.maxLength(100)]],
    employmentType: ['FullTime' as EmploymentType, Validators.required],
    salaryMin: [null as number | null, [Validators.required, Validators.min(0.01)]],
    salaryMax: [null as number | null, [Validators.required, Validators.min(0.01)]],
    description: ['', [Validators.required, Validators.maxLength(4000)]],
    closingDate: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const posting = this.current();
      if (!posting) {
        return;
      }

      const { title, location, employmentType, salaryMin, salaryMax, description, closingDate } = posting;
      this.form.reset({ title, location, employmentType, salaryMin, salaryMax, description, closingDate });

      if (posting.status === 'Closed' || posting.status === 'PendingApproval') {
        this.form.disable();
      } else {
        this.form.enable();
      }
    });

    effect(() => {
      const error = this.posting.error();
      if (error) {
        this.notifier.error(error);
        if (error instanceof HttpErrorResponse && error.status === 404) {
          void this.router.navigateByUrl('/job-postings');
        }
      }
    });
  }

  protected stepState(step: JobPostingStatus): 'done' | 'current' | 'upcoming' {
    const status = this.current()?.status;
    if (!status) {
      return 'upcoming';
    }

    const position = STATUSES.indexOf(step) - STATUSES.indexOf(status);
    return position < 0 ? 'done' : position === 0 ? 'current' : 'upcoming';
  }

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.notifier.message('Some fields need attention. Check the messages under each field.');
      return;
    }

    const input = this.form.getRawValue() as JobPostingInput; // salaries are non-null once the form is valid
    const id = this.id();

    if (this.isCreate() || !id) {
      this.run(this.service.create(input), 'Posting created', (saved) =>
        this.router.navigate(['/job-postings', saved.id]),
      );
    } else {
      this.run(this.service.update(id, input), 'Changes saved', (saved) => {
        this.posting.set(saved);
        if (this.mode() === 'edit') {
          void this.router.navigate(['/job-postings', saved.id]);
        }
      });
    }
  }

  protected submit(posting: JobPosting): void {
    this.run(this.service.submit(posting.id), 'Sent for approval', (saved) => this.posting.set(saved));
  }

  protected close(posting: JobPosting): void {
    this.confirm({
      title: 'Close this posting?',
      message: `"${posting.title}" will stop accepting candidates. Closed postings can't be edited or reopened.`,
      confirmLabel: 'Close posting',
    })
      .pipe(switchMap(() => this.withBusy(this.service.close(posting.id))))
      .subscribe({
        next: (saved) => {
          this.posting.set(saved);
          this.notifier.success('Posting closed');
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  protected delete(posting: JobPosting): void {
    this.confirm({
      title: 'Delete this draft?',
      message: `"${posting.title}" will be deleted permanently. This can't be undone.`,
      confirmLabel: 'Delete draft',
    })
      .pipe(switchMap(() => this.withBusy(this.service.delete(posting.id))))
      .subscribe({
        next: () => {
          this.notifier.success('Draft deleted');
          void this.router.navigateByUrl('/job-postings');
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  private confirm(data: ConfirmDialogData): Observable<true> {
    return this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, width: '420px', maxWidth: '90vw' })
      .afterClosed()
      .pipe(filter((confirmed): confirmed is true => confirmed === true));
  }

  private withBusy<T>(request: Observable<T>): Observable<T> {
    return defer(() => {
      this.busy.set(true);
      return request.pipe(finalize(() => this.busy.set(false)));
    });
  }

  private run(request: Observable<JobPosting>, message: string, then: (saved: JobPosting) => unknown): void {
    this.withBusy(request).subscribe({
      next: (saved) => {
        this.notifier.success(message);
        then(saved);
      },
      error: (error: unknown) => {
        if (applyServerErrors(this.form, error)) {
          this.notifier.message('Some fields need attention. Check the messages under each field.');
        } else {
          this.notifier.error(error);
        }
      },
    });
  }
}
