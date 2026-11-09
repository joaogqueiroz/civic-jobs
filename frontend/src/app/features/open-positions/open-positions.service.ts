import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PublicJobPosting } from '../../core/models';

@Injectable({ providedIn: 'root' })
export class OpenPositionsService {
  private readonly http = inject(HttpClient);

  /** Open postings from every department that close today or later; no login needed (FR-11). */
  list(): Observable<PublicJobPosting[]> {
    return this.http.get<PublicJobPosting[]>('/jobs/api/public/job-postings');
  }
}
