import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, effect, inject } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Notifier } from '../../core/notifier';
import { employmentTypeLabel } from '../job-postings/labels';
import { OpenPositionsService } from './open-positions.service';

/** Public list of open positions for candidates, no login needed (FR-11). */
@Component({
  selector: 'app-open-positions-page',
  imports: [CurrencyPipe, DatePipe, MatProgressBarModule],
  templateUrl: './open-positions-page.html',
  styleUrl: './open-positions-page.scss',
})
export class OpenPositionsPage {
  private readonly service = inject(OpenPositionsService);
  private readonly notifier = inject(Notifier);

  protected readonly employmentTypeLabel = employmentTypeLabel;
  protected readonly positions = rxResource({ stream: () => this.service.list() });

  constructor() {
    effect(() => {
      const error = this.positions.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }
}
