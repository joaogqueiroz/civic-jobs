import { UserRole } from '../core/models';

/** How each role reads on screen; the API sends the enum names. */
export const ROLE_LABELS: Record<UserRole, string> = {
  Admin: 'Admin',
  Recruiter: 'Recruiter',
  HiringManager: 'Hiring manager',
  Candidate: 'Candidate',
};
