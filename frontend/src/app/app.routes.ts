import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { roleGuard } from './core/auth.guard';
import { AuthService } from './core/auth.service';

const jobPostingPage = () =>
  import('./features/job-postings/job-posting-page').then((m) => m.JobPostingPage);

/** "/" and unknown URLs go to the first page of the user's role, or to the public list when logged out. */
const homeRedirect = () => inject(Router).parseUrl(inject(AuthService).home());

export const routes: Routes = [
  { path: '', pathMatch: 'full', children: [], canActivate: [homeRedirect] },
  {
    path: 'login',
    title: 'Log in · CivicJobs',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Create an account · CivicJobs',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'open-positions',
    title: 'Open positions · CivicJobs',
    loadComponent: () =>
      import('./features/open-positions/open-positions-page').then((m) => m.OpenPositionsPage),
  },
  {
    path: 'my-applications',
    title: 'My applications · CivicJobs',
    canActivate: [roleGuard('Candidate')],
    loadComponent: () =>
      import('./features/applications/my-applications-page').then((m) => m.MyApplicationsPage),
  },
  {
    path: 'job-postings',
    canActivate: [roleGuard('Recruiter')],
    children: [
      {
        path: '',
        title: 'My job postings · CivicJobs',
        loadComponent: () =>
          import('./features/job-postings/job-postings-list-page').then((m) => m.JobPostingsListPage),
      },
      { path: 'new', title: 'New posting · CivicJobs', data: { mode: 'create' }, loadComponent: jobPostingPage },
      { path: ':id', title: 'Job posting · CivicJobs', data: { mode: 'view' }, loadComponent: jobPostingPage },
      { path: ':id/edit', title: 'Edit posting · CivicJobs', data: { mode: 'edit' }, loadComponent: jobPostingPage },
    ],
  },
  {
    path: 'department',
    canActivate: [roleGuard('HiringManager')],
    children: [
      {
        path: '',
        title: 'Department postings · CivicJobs',
        loadComponent: () =>
          import('./features/department/department-postings-page').then((m) => m.DepartmentPostingsPage),
      },
      {
        path: ':id',
        title: 'Department posting · CivicJobs',
        loadComponent: () =>
          import('./features/department/department-posting-page').then((m) => m.DepartmentPostingPage),
      },
    ],
  },
  {
    path: 'admin',
    title: 'Agency admin · CivicJobs',
    canActivate: [roleGuard('Admin')],
    loadComponent: () => import('./features/admin/admin-page').then((m) => m.AdminPage),
  },
  { path: '**', children: [], canActivate: [homeRedirect] },
];
