import { Component, inject } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

export interface TextDialogData {
  title: string;
  message: string;
  label: string;
  hint: string;
  confirmLabel: string;
  maxLength: number;
  required: boolean;
}

/**
 * Asks for one block of text (a rejection reason, a cover letter). Closes with the trimmed text,
 * or with undefined when cancelled.
 */
@Component({
  selector: 'app-text-dialog',
  imports: [ReactiveFormsModule, MatDialogModule, MatButtonModule, MatFormFieldModule, MatInputModule],
  template: `
    <h2 mat-dialog-title>{{ data.title }}</h2>
    <form [formGroup]="form" (ngSubmit)="confirm()" novalidate>
      <mat-dialog-content>
        <p class="message">{{ data.message }}</p>
        <mat-form-field appearance="outline" class="field">
          <mat-label>{{ data.label }}</mat-label>
          <textarea matInput rows="5" formControlName="text" [attr.maxlength]="data.maxLength"></textarea>
          <mat-hint>{{ data.hint }}</mat-hint>
          @if (form.controls.text.hasError('required') || form.controls.text.hasError('pattern')) {
            <mat-error>{{ data.label }} is required.</mat-error>
          } @else if (form.controls.text.hasError('maxlength')) {
            <mat-error>Use at most {{ data.maxLength }} characters.</mat-error>
          }
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" [mat-dialog-close]="undefined">Cancel</button>
        <button mat-flat-button type="submit">{{ data.confirmLabel }}</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: `
    .message {
      margin: 0 0 16px;
    }
    .field {
      width: 100%;
    }
  `,
})
export class TextDialog {
  protected readonly data = inject<TextDialogData>(MAT_DIALOG_DATA);
  private readonly dialogRef = inject<MatDialogRef<TextDialog, string>>(MatDialogRef);

  protected readonly form = inject(NonNullableFormBuilder).group({
    text: [
      '',
      this.data.required
        ? [Validators.required, Validators.pattern(/\S/), Validators.maxLength(this.data.maxLength)]
        : [Validators.maxLength(this.data.maxLength)],
    ],
  });

  protected confirm(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.dialogRef.close(this.form.controls.text.value.trim());
  }
}
