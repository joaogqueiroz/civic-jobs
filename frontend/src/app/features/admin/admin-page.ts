import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, signal } from '@angular/core';
import { rxResource } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { Observable, defer, finalize } from 'rxjs';
import { applyServerErrors, problemMessage } from '../../core/api-errors';
import { AuthService } from '../../core/auth.service';
import { Staff, UserRole } from '../../core/models';
import { Notifier } from '../../core/notifier';
import { formFieldDefaults } from '../../shared/form-field-defaults';
import { ROLE_LABELS } from '../../shared/role-labels';
import { AdminService } from './admin.service';

type StaffRole = Exclude<UserRole, 'Candidate'>;

const STAFF_ROLES: readonly StaffRole[] = ['Recruiter', 'HiringManager', 'Admin'];

/** Recruiters and hiring managers work in a department; admins work for the whole agency (BR-13). */
const needsDepartment = (role: UserRole) => role === 'Recruiter' || role === 'HiringManager';

/** The agency admin's page: departments, staff accounts and their roles (FR-12 to FR-14). */
@Component({
  selector: 'app-admin-page',
  imports: [ReactiveFormsModule, MatButtonModule, MatFormFieldModule, MatInputModule, MatProgressBarModule, MatSelectModule],
  providers: [formFieldDefaults],
  templateUrl: './admin-page.html',
  styleUrl: './admin-page.scss',
})
export class AdminPage {
  private readonly service = inject(AdminService);
  private readonly notifier = inject(Notifier);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly auth = inject(AuthService);

  protected readonly roles = STAFF_ROLES;
  protected readonly roleLabels = ROLE_LABELS;
  protected readonly needsDepartment = needsDepartment;
  protected readonly busy = signal(false);
  /** The staff member whose role is being changed, if any. */
  protected readonly editingId = signal<string | null>(null);

  protected readonly departments = rxResource({ stream: () => this.service.departments() });
  protected readonly staff = rxResource({ stream: () => this.service.staff() });
  protected readonly departmentList = computed(() => (this.departments.hasValue() ? this.departments.value() : []));

  protected readonly departmentForm = this.fb.group({
    name: ['', [Validators.required, Validators.maxLength(100)]],
  });

  // Mirrors the API rules (BR-02, BR-13) so most mistakes show before the request; the API still validates.
  protected readonly staffForm = this.fb.group({
    fullName: ['', [Validators.required, Validators.maxLength(100)]],
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: [
      '',
      [Validators.required, Validators.minLength(8), Validators.maxLength(128), Validators.pattern(/^(?=.*[A-Za-z])(?=.*\d).+$/)],
    ],
    role: ['Recruiter' as StaffRole, Validators.required],
    departmentId: [null as string | null],
  });

  protected readonly roleForm = this.fb.group({
    role: ['Recruiter' as StaffRole, Validators.required],
    departmentId: [null as string | null],
  });

  constructor() {
    effect(() => {
      const error = this.departments.error() ?? this.staff.error();
      if (error) {
        this.notifier.error(error);
      }
    });
  }

  protected addDepartment(): void {
    if (this.departmentForm.invalid) {
      this.departmentForm.markAllAsTouched();
      return;
    }

    this.withBusy(this.service.createDepartment(this.departmentForm.getRawValue().name)).subscribe({
      next: (department) => {
        this.departments.update((list) =>
          [...(list ?? []), department].sort((a, b) => a.name.localeCompare(b.name)),
        );
        this.departmentForm.reset();
        this.notifier.success(`${department.name} added`);
      },
      error: (error: unknown) => {
        // A duplicate or invalid name belongs next to the field; anything else is not about the name.
        if (error instanceof HttpErrorResponse && (error.status === 400 || error.status === 409)) {
          this.showFieldError(this.departmentForm.controls.name, error);
        } else {
          this.notifier.error(error);
        }
      },
    });
  }

  protected addStaff(): void {
    const input = this.staffForm.getRawValue();
    if (!this.departmentIsValid(input.role, input.departmentId) || this.staffForm.invalid) {
      this.staffForm.markAllAsTouched();
      return;
    }

    const departmentId = needsDepartment(input.role) ? input.departmentId : null;
    this.withBusy(this.service.createStaff({ ...input, departmentId })).subscribe({
      next: (staff) => {
        this.staff.update((list) => [...(list ?? []), staff].sort((a, b) => a.fullName.localeCompare(b.fullName)));
        this.staffForm.reset();
        this.notifier.success(`Account created for ${staff.fullName}. Share the password with them.`);
      },
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 409) {
          this.showFieldError(this.staffForm.controls.email, error);
        } else if (!applyServerErrors(this.staffForm, error)) {
          this.notifier.error(error);
        }
      },
    });
  }

  protected startEditing(member: Staff): void {
    this.roleForm.reset({ role: member.role as StaffRole, departmentId: member.departmentId });
    this.editingId.set(member.id);
  }

  protected saveRole(member: Staff): void {
    const input = this.roleForm.getRawValue();
    if (!this.departmentIsValid(input.role, input.departmentId)) {
      this.roleForm.controls.departmentId.markAsTouched();
      return;
    }

    const departmentId = needsDepartment(input.role) ? input.departmentId : null;
    this.withBusy(this.service.changeRole(member.id, { role: input.role, departmentId })).subscribe({
      next: (saved) => {
        this.staff.update((list) => list?.map((item) => (item.id === saved.id ? saved : item)));
        this.editingId.set(null);
        this.notifier.success(`${saved.fullName} is now ${ROLE_LABELS[saved.role].toLowerCase()}. It applies at their next login.`);
      },
      error: (error: unknown) => this.notifier.error(error),
    });
  }

  /** The department select is only shown (and required) for recruiters and hiring managers. */
  private departmentIsValid(role: UserRole, departmentId: string | null): boolean {
    return !needsDepartment(role) || !!departmentId;
  }

  private showFieldError(control: { setErrors(errors: object): void; markAsTouched(): void }, error: unknown): void {
    control.setErrors({ server: problemMessage(error) });
    control.markAsTouched();
  }

  private withBusy<T>(request: Observable<T>): Observable<T> {
    return defer(() => {
      this.busy.set(true);
      return request.pipe(finalize(() => this.busy.set(false)));
    });
  }
}
