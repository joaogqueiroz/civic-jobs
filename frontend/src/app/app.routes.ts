import { Routes } from '@angular/router';

export const routes: Routes = [
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
  { path: '**', redirectTo: 'login' },
];
