import { HttpErrorResponse } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Router, RouterLink } from '@angular/router';
import { Observable, defer, filter, finalize, switchMap } from 'rxjs';
import { applyServerErrors } from '../../core/api-errors';
import { EmploymentType, JobPosting, JobPostingInput } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { ConfirmDialog, ConfirmDialogData } from '../../shared/confirm-dialog';
import { StatusBadge } from '../../shared/status-badge';
import { JobPostingsService } from './job-postings.service';
import { EMPLOYMENT_TYPES, employmentTypeLabel } from './labels';

export type JobPostingPageMode = 'create' | 'view' | 'edit';

/** One page for a posting: create (/new), detail with actions (/:id) and edit (/:id/edit). */
@Component({
  selector: 'app-job-posting-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    CurrencyPipe,
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatSelectModule,
    StatusBadge,
  ],
  templateUrl: './job-posting-page.html',
  styleUrl: './job-posting-page.scss',
})
export class JobPostingPage {
  private readonly service = inject(JobPostingsService);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);

  /** From the route data. */
  readonly mode = input.required<JobPostingPageMode>();
  /** From the route parameter (absent when creating). */
  readonly id = input<string>();

  protected readonly employmentTypes = EMPLOYMENT_TYPES;
  protected readonly employmentTypeLabel = employmentTypeLabel;
  protected readonly today = new Date().toLocaleDateString('en-CA'); // yyyy-MM-dd, local date
  protected readonly busy = signal(false);

  protected readonly posting = rxResource({
    params: () => (this.mode() === 'create' ? undefined : this.id()),
    stream: ({ params: id }) => this.service.get(id),
  });

  protected readonly current = computed(() => (this.posting.hasValue() ? this.posting.value() : undefined));
  protected readonly canEdit = computed(() => this.isStatus('Draft', 'Open'));
  protected readonly canPublish = computed(() => this.isStatus('Draft'));
  protected readonly canClose = computed(() => this.isStatus('Open'));
  protected readonly canDelete = computed(() => this.isStatus('Draft'));

  protected readonly form = inject(NonNullableFormBuilder).group({
    title: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    department: ['', [Validators.required, Validators.maxLength(100)]],
    description: ['', [Validators.required, Validators.maxLength(4000)]],
    location: ['', [Validators.required, Validators.maxLength(100)]],
    employmentType: ['FullTime' as EmploymentType, Validators.required],
    salaryMin: [0, [Validators.required, Validators.min(0.01)]],
    salaryMax: [0, [Validators.required, Validators.min(0.01)]],
    closingDate: ['', Validators.required],
  });

  constructor() {
    effect(() => {
      const posting = this.current();
      if (posting && this.mode() === 'edit') {
        const { title, department, description, location, employmentType, salaryMin, salaryMax, closingDate } =
          posting;
        this.form.reset({ title, department, description, location, employmentType, salaryMin, salaryMax, closingDate });
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

  protected save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const input: JobPostingInput = this.form.getRawValue();
    const id = this.id();
    const request = this.mode() === 'edit' && id ? this.service.update(id, input) : this.service.create(input);

    this.run(request, this.mode() === 'edit' ? 'Posting saved.' : 'Draft created.', (saved) =>
      this.router.navigate(['/job-postings', saved.id]),
    );
  }

  protected publish(posting: JobPosting): void {
    this.run(this.service.publish(posting.id), 'Posting published.', (saved) => this.posting.set(saved));
  }

  protected close(posting: JobPosting): void {
    this.confirm({
      title: 'Close this posting?',
      message: `"${posting.title}" will stop accepting candidates. A closed posting can't be edited or reopened.`,
      confirmLabel: 'Close posting',
    })
      .pipe(switchMap(() => this.withBusy(this.service.close(posting.id))))
      .subscribe({
        next: (saved) => {
          this.posting.set(saved);
          this.notifier.success('Posting closed.');
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  protected delete(posting: JobPosting): void {
    this.confirm({
      title: 'Delete this draft?',
      message: `"${posting.title}" will be permanently deleted.`,
      confirmLabel: 'Delete',
    })
      .pipe(switchMap(() => this.withBusy(this.service.delete(posting.id))))
      .subscribe({
        next: () => {
          this.notifier.success('Draft deleted.');
          void this.router.navigateByUrl('/job-postings');
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  private isStatus(...statuses: JobPosting['status'][]): boolean {
    const posting = this.current();
    return !!posting && statuses.includes(posting.status);
  }

  private confirm(data: ConfirmDialogData): Observable<true> {
    return this.dialog
      .open<ConfirmDialog, ConfirmDialogData, boolean>(ConfirmDialog, { data, width: '400px', maxWidth: '90vw' })
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
        if (!applyServerErrors(this.form, error)) {
          this.notifier.error(error);
        }
      },
    });
  }
}
