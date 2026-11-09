import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { applyServerErrors, problemMessage } from '../../core/api-errors';
import { AuthService } from '../../core/auth.service';
import { Notifier } from '../../core/notifier';
import { formFieldDefaults } from '../../shared/form-field-defaults';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, MatFormFieldModule, MatInputModule, MatButtonModule],
  providers: [formFieldDefaults],
  templateUrl: './register-page.html',
  styleUrl: './auth-page.scss',
})
export class RegisterPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly notifier = inject(Notifier);

  readonly submitting = signal(false);

  // Mirrors the API rules (BR-02) so most mistakes show before the request; the API still validates.
  readonly form = inject(NonNullableFormBuilder).group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: [
      '',
      [
        Validators.required,
        Validators.minLength(8),
        Validators.maxLength(128),
        Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/),
      ],
    ],
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const input = this.form.getRawValue();
    this.submitting.set(true);

    this.auth
      .register(input)
      .pipe(
        switchMap(() => this.auth.login({ email: input.email, password: input.password })),
        switchMap(() => this.auth.loadCurrentUser()),
      )
      .subscribe({
        next: () => {
          this.notifier.success('Account created');
          void this.router.navigateByUrl('/job-postings');
        },
        error: (error: unknown) => {
          this.submitting.set(false);
          if (error instanceof HttpErrorResponse && error.status === 409) {
            // The email is already registered: show it on the field the user has to change.
            this.form.controls.email.setErrors({ server: problemMessage(error) });
            this.form.controls.email.markAsTouched();
          } else if (!applyServerErrors(this.form, error)) {
            this.notifier.error(error);
          }
        },
      });
  }
}
