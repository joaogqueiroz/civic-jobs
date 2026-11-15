import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { CandidateApplication } from '../../core/models';

const API = '/jobs/api/applications';

/** The logged-in candidate's applications (FR-18, FR-19). */
@Injectable({ providedIn: 'root' })
export class ApplicationsService {
  private readonly http = inject(HttpClient);

  listMine(): Observable<CandidateApplication[]> {
    return this.http.get<CandidateApplication[]>(API);
  }

  apply(jobPostingId: string, coverLetter: string | null): Observable<CandidateApplication> {
    return this.http.post<CandidateApplication>(API, { jobPostingId, coverLetter });
  }
}
