import { Provider } from '@angular/core';
import { MAT_FORM_FIELD_DEFAULT_OPTIONS } from '@angular/material/form-field';

/**
 * Every field in the app is required, so the asterisks add noise. Provided by each form page
 * (not app-wide) so Material's form field stays out of the initial bundle.
 */
export const formFieldDefaults: Provider = {
  provide: MAT_FORM_FIELD_DEFAULT_OPTIONS,
  useValue: { appearance: 'outline', hideRequiredMarker: true },
};
