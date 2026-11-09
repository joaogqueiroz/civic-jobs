import { Component, computed, input } from '@angular/core';
import { JobPostingStatus } from '../core/models';

/** A small pill with a dot and the status name, so the status never relies on color alone. */
@Component({
  selector: 'app-status-badge',
  template: `<span class="badge" [class]="tone()"><span class="dot" aria-hidden="true"></span>{{ status() }}</span>`,
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
    .draft {
      background: #ecefee;
      color: #4a555c;
    }
    .open {
      background: #e3f1ea;
      color: #1e5b47;
    }
    .closed {
      background: #e9ecef;
      color: #5b6770;
    }
  `,
})
export class StatusBadge {
  readonly status = input.required<JobPostingStatus>();

  protected readonly tone = computed(() => `badge ${this.status().toLowerCase()}`);
}
