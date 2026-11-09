// Mirrors the API contract in docs/user-story.md (ViewModels and InputModels, camelCase JSON, enums as strings).

export type JobPostingStatus = 'Draft' | 'Open' | 'Closed';

export type EmploymentType = 'FullTime' | 'PartTime' | 'Temporary';

export interface User {
  id: string;
  email: string;
  fullName: string;
  createdAt: string;
}

export interface AuthToken {
  accessToken: string;
  expiresAt: string;
}

export interface LoginInput {
  email: string;
  password: string;
}

export interface RegisterInput {
  fullName: string;
  email: string;
  password: string;
}

export interface JobPosting {
  id: string;
  title: string;
  department: string;
  description: string;
  location: string;
  employmentType: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  /** ISO date, yyyy-MM-dd. */
  closingDate: string;
  status: JobPostingStatus;
  createdAt: string;
  updatedAt: string;
}

/** A posting in the public list for candidates (FR-11): no status, owner or timestamps. */
export interface PublicJobPosting {
  id: string;
  title: string;
  department: string;
  description: string;
  location: string;
  employmentType: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  /** ISO date, yyyy-MM-dd. */
  closingDate: string;
}

export interface JobPostingInput {
  title: string;
  department: string;
  description: string;
  location: string;
  employmentType: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  closingDate: string;
}

/** RFC 7807 body returned by both APIs for every error; `errors` is present on validation failures (400). */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
