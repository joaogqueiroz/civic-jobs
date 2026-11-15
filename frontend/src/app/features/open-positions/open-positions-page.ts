import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { filter, finalize, switchMap } from 'rxjs';
import { AuthService } from '../../core/auth.service';
import { PublicJobPosting } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { TextDialog, TextDialogData } from '../../shared/text-dialog';
import { ApplicationsService } from '../applications/applications.service';
import { employmentTypeLabel } from '../job-postings/labels';
import { OpenPositionsService } from './open-positions.service';

/** Public list of open positions, no login needed (FR-11); candidates can apply from here (FR-18). */
@Component({
  selector: 'app-open-positions-page',
  imports: [RouterLink, CurrencyPipe, DatePipe, MatButtonModule, MatIconModule, MatProgressBarModule],
  templateUrl: './open-positions-page.html',
  styleUrl: './open-positions-page.scss',
})
export class OpenPositionsPage {
  private readonly service = inject(OpenPositionsService);
  private readonly applicationsService = inject(ApplicationsService);
  private readonly dialog = inject(MatDialog);
  private readonly notifier = inject(Notifier);
  protected readonly auth = inject(AuthService);

  protected readonly employmentTypeLabel = employmentTypeLabel;
  protected readonly isCandidate = computed(() => this.auth.role() === 'Candidate');
  protected readonly applyingTo = signal<string | null>(null);

  protected readonly positions = rxResource({ stream: () => this.service.list() });

  /** The ids the candidate already applied to, so the button says "Applied" (one application per posting, BR-18). */
  private readonly mine = rxResource({
    params: () => this.isCandidate() || undefined,
    stream: () => this.applicationsService.listMine(),
  });

  protected readonly appliedIds = computed(
    () => new Set(this.mine.hasValue() ? this.mine.value().map((application) => application.jobPostingId) : []),
  );

  constructor() {
    effect(() => {
      const error = this.positions.error() ?? this.mine.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }

  protected apply(position: PublicJobPosting): void {
    const data: TextDialogData = {
      title: `Apply to ${position.title}`,
      message: `${position.department} will see your name, email and the note below.`,
      label: 'Cover letter (optional)',
      hint: 'Why are you a good fit? Up to 2,000 characters.',
      confirmLabel: 'Send application',
      maxLength: 2000,
      required: false,
    };

    this.dialog
      .open<TextDialog, TextDialogData, string>(TextDialog, { data, width: '520px', maxWidth: '90vw' })
      .afterClosed()
      .pipe(
        filter((coverLetter): coverLetter is string => coverLetter !== undefined),
        switchMap((coverLetter) => {
          this.applyingTo.set(position.id);
          return this.applicationsService
            .apply(position.id, coverLetter || null)
            .pipe(finalize(() => this.applyingTo.set(null)));
        }),
      )
      .subscribe({
        next: (application) => {
          this.mine.update((list) => [application, ...(list ?? [])]);
          this.notifier.success('Application sent. We emailed you a confirmation.');
        },
        error: (error: unknown) => this.notifier.error(error),
      });
  }
}
