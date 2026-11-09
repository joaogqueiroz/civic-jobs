import { Component, input } from '@angular/core';
import { JobPostingStatus } from '../core/models';

@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="status().toLowerCase()">{{ status() }}</span>`,
  styles: `
    .badge {
      display: inline-block;
      padding: 2px 10px;
      border-radius: 12px;
      font: var(--mat-sys-label-medium);
      white-space: nowrap;
    }
    .draft {
      background: var(--mat-sys-surface-container-highest);
      color: var(--mat-sys-on-surface-variant);
    }
    .open {
      background: var(--mat-sys-primary-container);
      color: var(--mat-sys-on-primary-container);
    }
    .closed {
      background: var(--mat-sys-tertiary-container);
      color: var(--mat-sys-on-tertiary-container);
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<JobPostingStatus>();
}
