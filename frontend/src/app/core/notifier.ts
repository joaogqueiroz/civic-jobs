import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { problemMessage } from './api-errors';

/** Snackbar feedback for success and errors. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
  }

  error(error: unknown): void {
    this.snackBar.open(problemMessage(error), 'Dismiss', { duration: 6000, politeness: 'assertive' });
  }
}
