import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ProblemDetails } from './models';

/** A short, user-facing message for any API error. */
export function problemMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 0) {
      return "Can't reach the server. Check that both APIs are running, then try again.";
    }

    if (error.status === 404) {
      return "This posting doesn't exist or isn't yours. Pick one from your job postings.";
    }

    const problem = error.error as ProblemDetails | null;
    return problem?.detail ?? problem?.title ?? `The request failed (${error.status}). Try again.`;
  }

  return 'Something went wrong. Try again.';
}

/**
 * Puts the API's validation errors (400 ValidationProblemDetails, camelCase keys) on the matching form controls
 * as a `server` error, so each message shows next to its field. Returns true when at least one field got an error.
 */
export function applyServerErrors(form: FormGroup, error: unknown): boolean {
  if (!(error instanceof HttpErrorResponse) || error.status !== 400) {
    return false;
  }

  const errors = (error.error as ProblemDetails | null)?.errors ?? {};
  let applied = false;

  for (const [field, messages] of Object.entries(errors)) {
    const control = form.get(field);
    if (control && messages.length > 0) {
      control.setErrors({ ...control.errors, server: messages.join(' ') });
      control.markAsTouched();
      applied = true;
    }
  }

  return applied;
}
