import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';

const jobPostingPage = () =>
  import('./features/job-postings/job-posting-page').then((m) => m.JobPostingPage);

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'job-postings' },
  {
    path: 'login',
    title: 'Log in · CivicJobs',
    loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
  },
  {
    path: 'register',
    title: 'Register · CivicJobs',
    loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
  },
  {
    path: 'job-postings',
    canActivate: [authGuard],
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
  { path: '**', redirectTo: 'job-postings' },
];
