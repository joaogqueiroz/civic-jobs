import { Component, computed, input } from '@angular/core';
import { JobApplicationStatus, JobPostingStatus } from '../core/models';

/** How each status reads on screen; the API sends the enum names. */
export const STATUS_LABELS: Record<JobPostingStatus | JobApplicationStatus, string> = {
  Draft: 'Draft',
  PendingApproval: 'Awaiting approval',
  Open: 'Open',
  Closed: 'Closed',
  Submitted: 'Submitted',
  UnderReview: 'Under review',
  Rejected: 'Not selected',
  Hired: 'Hired',
};

/** A small pill with a dot and the status name, so the status never relies on color alone. */
@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="tone()"><span class="dot" aria-hidden="true"></span>{{ label() }}</span>`,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 2px 10px 2px 8px;
      border-radius: 999px;
      font-size: 14px;
      font-weight: 600;
      line-height: 1.5;
      white-space: nowrap;
    }
    .dot {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: currentColor;
    }
    .draft,
    .submitted {
      background: #ecefee;
      color: #4a555c;
    }
    .pendingapproval,
    .underreview {
      background: #fbf1dc;
      color: #7a4f05;
    }
    .open,
    .hired {
      background: #e3f1ea;
      color: #1e5b47;
    }
    .closed {
      background: #e9ecef;
      color: #5b6770;
    }
    .rejected {
      background: #fbeceb;
      color: #8f342c;
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<JobPostingStatus | JobApplicationStatus>();

  protected readonly label = computed(() => STATUS_LABELS[this.status()]);
  protected readonly tone = computed(() => `badge ${this.status().toLowerCase()}`);
}
