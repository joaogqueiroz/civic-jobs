import { Component, inject, input, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { applyServerErrors } from '../../core/api-errors';
import { AuthService } from '../../core/auth.service';
import { Notifier } from '../../core/notifier';
import { formFieldDefaults } from '../../shared/form-field-defaults';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  providers: [formFieldDefaults],
  templateUrl: './login-page.html',
  styleUrl: './auth-page.scss',
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  /** Where the guard sent the user from (query parameter). */
  readonly returnUrl = input<string>();

  readonly submitting = signal(false);
  readonly credentialsError = signal<string | null>(null);

  readonly form = inject(NonNullableFormBuilder).group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', Validators.required],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.submitting.set(true);
    this.credentialsError.set(null);

    this.auth.login(this.form.getRawValue()).subscribe({
        // Back to where the guard stopped the user, or to the first page of their role.
        next: () => void this.router.navigateByUrl(this.returnUrl() || this.auth.home()),
        error: (error: unknown) => {
          this.submitting.set(false);
          if (error instanceof HttpErrorResponse && error.status === 401) {
            this.credentialsError.set("That email and password don't match an account. Check them and try again.");
          } else if (!applyServerErrors(this.form, error)) {
            this.notifier.error(error);
          }
        },
      });
  }
}
