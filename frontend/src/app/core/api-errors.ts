import { HttpErrorResponse } from '@angular/common/http';
import { FormGroup } from '@angular/forms';
import { ProblemDetails } from './models';

/**
 * A plain-language message for any error: what happened and what to do next. Never shows status codes.
 * For 400 and 409 the API's own message is used, because those are written for people
 * (for example "This posting is already published.").
 */
export function problemMessage(error: unknown): string {
  if (!(error instanceof HttpErrorResponse)) {
    return 'Something went wrong. Try again.';
  }

  const detail = (error.error as ProblemDetails | null)?.detail;

  switch (error.status) {
    case 0:
      return "Can't reach CivicJobs right now. Check your internet connection and try again.";
    case 400:
      return detail ?? "Some details aren't valid. Check the form and try again.";
    case 401:
      return 'Your session has ended. Log in again to continue.';
    case 403:
      return "You don't have permission to do that.";
    case 404:
      return detail ?? "We couldn't find what you were looking for. It may have been deleted.";
    case 409:
      return detail ?? "That change isn't possible right now. Reload the page and try again.";
    default:
      return error.status >= 500
        ? 'Something went wrong on our side. Try again in a moment.'
        : 'Something went wrong. Try again.';
  }
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
