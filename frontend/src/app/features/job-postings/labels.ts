import { EmploymentType, JobPostingStatus } from '../../core/models';

/** The life cycle of a posting, in order (BR-06). */
export const STATUSES: readonly JobPostingStatus[] = ['Draft', 'PendingApproval', 'Open', 'Closed'];

export const EMPLOYMENT_TYPES: readonly { value: EmploymentType; label: string }[] = [
  { value: 'FullTime', label: 'Full time' },
  { value: 'PartTime', label: 'Part time' },
  { value: 'Temporary', label: 'Temporary' },
];

export function employmentTypeLabel(value: EmploymentType): string {
  return EMPLOYMENT_TYPES.find((type) => type.value === value)?.label ?? value;
}
