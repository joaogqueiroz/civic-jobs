import { inject, Injectable } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { problemMessage } from './api-errors';

/** Snackbar feedback. Success messages reuse the verb of the button that triggered them. */
@Injectable({ providedIn: 'root' })
export class Notifier {
  private readonly snackBar = inject(MatSnackBar);

  success(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 3000 });
  }

  message(message: string): void {
    this.snackBar.open(message, 'OK', { duration: 5000 });
  }

  error(error: unknown): void {
    this.snackBar.open(problemMessage(error), 'Dismiss', { duration: 8000, politeness: 'assertive' });
  }
}
