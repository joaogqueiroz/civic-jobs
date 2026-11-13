import { HttpClient, HttpParams } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { JobPosting, JobPostingInput, JobPostingStatus } from '../../core/models';

const API = '/jobs/api/job-postings';

@Injectable({ providedIn: 'root' })
export class JobPostingsService {
  private readonly http = inject(HttpClient);

  list(status?: JobPostingStatus): Observable<JobPosting[]> {
    const params = status ? new HttpParams().set('status', status) : undefined;
    return this.http.get<JobPosting[]>(API, { params });
  }

  get(id: string): Observable<JobPosting> {
    return this.http.get<JobPosting>(`${API}/${id}`);
  }

  create(input: JobPostingInput): Observable<JobPosting> {
    return this.http.post<JobPosting>(API, input);
  }

  update(id: string, input: JobPostingInput): Observable<JobPosting> {
    return this.http.put<JobPosting>(`${API}/${id}`, input);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API}/${id}`);
  }

  /** Sends a draft to the department's hiring manager (FR-08). */
  submit(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${API}/${id}/submit`, null);
  }

  close(id: string): Observable<JobPosting> {
    return this.http.post<JobPosting>(`${API}/${id}/close`, null);
  }
}
