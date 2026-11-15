import { DatePipe } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { RouterLink } from '@angular/router';
import { JobApplicationStatus } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { StatusBadge } from '../../shared/status-badge';
import { ApplicationsService } from './applications.service';

/** What each status means for the candidate, in plain words. */
const NEXT_STEP: Record<JobApplicationStatus, string> = {
  Submitted: 'The hiring manager will review it soon.',
  UnderReview: 'The hiring manager is reviewing your application.',
  Rejected: 'The department chose another candidate this time.',
  Hired: 'Congratulations! The department will contact you about next steps.',
};

/** The candidate's applications and their status (FR-19). */
@Component({
  selector: 'app-my-applications-page',
  imports: [RouterLink, DatePipe, MatButtonModule, MatProgressBarModule, StatusBadge],
  templateUrl: './my-applications-page.html',
  styleUrl: './my-applications-page.scss',
})
export class MyApplicationsPage {
  private readonly service = inject(ApplicationsService);
  private readonly notifier = inject(Notifier);

  protected readonly nextStep = NEXT_STEP;
  protected readonly applications = rxResource({ stream: () => this.service.listMine() });

  constructor() {
    effect(() => {
      const error = this.applications.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }
}
