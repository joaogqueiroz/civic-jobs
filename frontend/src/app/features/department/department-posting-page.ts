import { CurrencyPipe, DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Router, RouterLink } from '@angular/router';
import { Observable, defer, filter, finalize, switchMap } from 'rxjs';
import { JobApplicationStatus, JobPosting, PostingApplication } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { StatusBadge } from '../../shared/status-badge';
import { TextDialog, TextDialogData } from '../../shared/text-dialog';
import { employmentTypeLabel } from '../job-postings/labels';
import { DepartmentService } from './department.service';

interface ApplicationAction {
  status: JobApplicationStatus;
  label: string;
  /** The snackbar text once it's done. */
  done: string;
  primary: boolean;
}

/** What the hiring manager can do next with an application, by its status (BR-19). */
const NEXT_ACTIONS: Record<JobApplicationStatus, readonly ApplicationAction[]> = {
  Submitted: [
    { status: 'UnderReview', label: 'Start review', done: 'moved to review', primary: true },
    { status: 'Rejected', label: 'Not selected', done: 'marked as not selected', primary: false },
  ],
  UnderReview: [
    { status: 'Hired', label: 'Hire', done: 'hired', primary: true },
    { status: 'Rejected', label: 'Not selected', done: 'marked as not selected', primary: false },
  ],
  Rejected: [],
  Hired: [],
};

/** One posting of the hiring manager's department: approve or reject it, and decide on its applications. */
@Component({
  selector: 'app-department-posting-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule, StatusBadge],
  templateUrl: './department-posting-page.html',
  styleUrl: './department-posting-page.scss',
})
export class DepartmentPostingPage {
  private readonly service = inject(DepartmentService);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  private readonly router = inject(Router);

  /** From the route parameter. */
  readonly id = input.required<string>();

  protected readonly employmentTypeLabel = employmentTypeLabel;
  protected readonly nextActions = NEXT_ACTIONS;
  protected readonly busy = signal(false);

  protected readonly posting = rxResource({
    params: () => this.id(),
    stream: ({ params: id }) => this.service.get(id),
  });

  protected readonly current = computed(() => (this.posting.hasValue() ? this.posting.value() : undefined));

  /** Applications only exist once a posting has been Open; none to load while it waits for approval. */
  protected readonly applications = rxResource({
    params: () => {
      const posting = this.current();
      return posting && posting.status !== 'PendingApproval' ? posting.id : undefined;
    },
    stream: ({ params: id }) => this.service.applications(id),
  });

  constructor() {
    effect(() => {
      const error = this.posting.error() ?? this.applications.error();
      if (error) {
        this.notifier.error(error);
        if (error instanceof HttpErrorResponse && error.status === 404) {
          void this.router.navigateByUrl('/department');
        }
      }
    });
  }

  protected approve(posting: JobPosting): void {
    this.withBusy(this.service.approve(posting.id)).subscribe({
      next: (saved) => {
        this.posting.set(saved);
        this.notifier.success('Posting approved. Candidates can see it now.');
      },
      error: (error: unknown) => this.notifier.error(error),
    });
  }

  protected reject(posting: JobPosting): void {
    const data: TextDialogData = {
      title: 'Ask for changes?',
      message: `"${posting.title}" goes back to the recruiter as a draft, with your note.`,
      label: 'What should change',
      hint: 'The recruiter sees this note on the posting. Up to 500 characters.',
      confirmLabel: 'Send back',
      maxLength: 500,
      required: true,
    };

    this.dialog
      .open<TextDialog, TextDialogData, string>(TextDialog, { data, width: '480px', maxWidth: '90vw' })
      .afterClosed()
      .pipe(
        filter((reason): reason is string => !!reason),
        switchMap((reason) => this.withBusy(this.service.reject(posting.id, reason))),
      )
      .subscribe({
        next: () => {
          // Back to Draft, the posting is no longer visible to the hiring manager.
          this.notifier.success('Sent back to the recruiter');
          void this.router.navigate(['/department'], { queryParams: { status: 'PendingApproval' } });
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }

  protected decide(application: PostingApplication, action: ApplicationAction): void {
    this.withBusy(this.service.changeApplicationStatus(application.id, action.status)).subscribe({
      next: (saved) => {
        this.applications.update((list) => list?.map((item) => (item.id === saved.id ? saved : item)));
        this.notifier.success(`${saved.candidateName} ${action.done}. We emailed the candidate.`);
      },
      error: (error: unknown) => this.notifier.error(error),
    });
  }

  private withBusy<T>(request: Observable<T>): Observable<T> {
    return defer(() => {
      this.busy.set(true);
      return request.pipe(finalize(() => this.busy.set(false)));
    });
  }
}
