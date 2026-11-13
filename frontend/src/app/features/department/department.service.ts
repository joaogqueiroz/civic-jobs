import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JobApplicationStatus, JobPosting, JobPostingStatus, PostingApplication } from '../../core/models';

const API = '/jobs/api/department';

/** The hiring manager's postings and applications, all limited to their department by the API (BR-16). */
@Injectable({ providedIn: 'root' })
export class DepartmentService {
  private readonly http = inject(HttpClient);

  list(status?: JobPostingStatus): Observable<JobPosting[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<JobPosting[]>(`${API}/job-postings`, { params });
  }

  get(id: string): Observable<JobPosting> {
    return this.http.get<JobPosting>(`${API}/job-postings/${id}`);
  }

  approve(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${API}/job-postings/${id}/approve`, null);
  }

  reject(id: string, reason: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${API}/job-postings/${id}/reject`, { reason });
  }

  applications(postingId: string): Observable<PostingApplication[]> {
    return this.http.get<PostingApplication[]>(`${API}/job-postings/${postingId}/applications`);
  }

  changeApplicationStatus(applicationId: string, status: JobApplicationStatus): Observable<PostingApplication> {
    return this.http.put<PostingApplication>(`${API}/applications/${applicationId}/status`, { status });
  }
}
