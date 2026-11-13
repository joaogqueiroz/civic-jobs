import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ChangeRoleInput, CreateStaffInput, Department, Staff } from '../../core/models';

const API = '/identity/api';

/** Departments and staff of the admin's agency (FR-12 to FR-14); the API limits both to that agency (BR-15). */
@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);

  departments(): Observable<Department[]> {
    return this.http.get<Department[]>(`${API}/departments`);
  }

  createDepartment(name: string): Observable<Department> {
    return this.http.post<Department>(`${API}/departments`, { name });
  }

  staff(): Observable<Staff[]> {
    return this.http.get<Staff[]>(`${API}/users`);
  }

  createStaff(input: CreateStaffInput): Observable<Staff> {
    return this.http.post<Staff>(`${API}/users`, input);
  }

  changeRole(userId: string, input: ChangeRoleInput): Observable<Staff> {
    return this.http.put<Staff>(`${API}/users/${userId}/role`, input);
  }
}
