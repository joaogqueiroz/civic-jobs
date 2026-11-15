// Mirrors the API contract in docs/user-story.md (ViewModels and InputModels, camelCase JSON, enums as strings).

export type JobPostingStatus = 'Draft' | 'PendingApproval' | 'Open' | 'Closed';

export type JobApplicationStatus = 'Submitted' | 'UnderReview' | 'Rejected' | 'Hired';

export type EmploymentType = 'FullTime' | 'PartTime' | 'Temporary';

export type UserRole = 'Admin' | 'Recruiter' | 'HiringManager' | 'Candidate';

export interface User {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  agencyId: string | null;
  agencyName: string | null;
  departmentId: string | null;
  departmentName: string | null;
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
  /** Why the hiring manager sent the posting back to Draft (BR-17). */
  rejectionReason: string | null;
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

/** The department comes from the recruiter's account, so it isn't part of the input (BR-13). */
export interface JobPostingInput {
  title: string;
  description: string;
  location: string;
  employmentType: EmploymentType;
  salaryMin: number;
  salaryMax: number;
  closingDate: string;
}

export interface Department {
  id: string;
  name: string;
  createdAt: string;
}

export interface Staff {
  id: string;
  email: string;
  fullName: string;
  role: UserRole;
  departmentId: string | null;
  departmentName: string | null;
  createdAt: string;
}

export interface CreateStaffInput {
  fullName: string;
  email: string;
  password: string;
  role: UserRole;
  departmentId: string | null;
}

export interface ChangeRoleInput {
  role: UserRole;
  departmentId: string | null;
}

/** An application as the candidate sees it (FR-19). */
export interface CandidateApplication {
  id: string;
  jobPostingId: string;
  jobTitle: string;
  department: string;
  location: string;
  status: JobApplicationStatus;
  appliedAt: string;
  updatedAt: string;
}

/** An application as the hiring manager sees it (FR-20). */
export interface PostingApplication {
  id: string;
  candidateId: string;
  candidateName: string;
  candidateEmail: string;
  coverLetter: string | null;
  status: JobApplicationStatus;
  appliedAt: string;
  updatedAt: string;
}

/** RFC 7807 body returned by both APIs for every error; `errors` is present on validation failures (400). */
export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}
