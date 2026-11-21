import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/home/home.page').then((m) => m.HomePage)
  },
  {
    path: 'dashboard',
    loadComponent: () =>
      import('./features/dashboard/dashboard.page').then((m) => m.DashboardPage)
  },
  {
    path: 'customers',
    loadComponent: () =>
      import('./features/customers/customers.page').then((m) => m.CustomersPage)
  },
  {
    path: '**',
    redirectTo: ''
  }
];
