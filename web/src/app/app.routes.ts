import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';
import { ShellComponent } from './layout/shell/shell.component';

export const routes: Routes = [
  {
    path: 'login',
    canActivate: [guestGuard],
    loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: '',
    canActivate: [authGuard],
    component: ShellComponent,
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
      {
        path: 'dashboard',
        loadComponent: () => import('./features/dashboard/dashboard.component').then(m => m.DashboardComponent)
      },
      {
        path: 'customers',
        loadComponent: () => import('./features/customers/customers.component').then(m => m.CustomersComponent)
      },
      {
        path: 'vehicles',
        loadComponent: () => import('./features/vehicles/vehicles.component').then(m => m.VehiclesComponent)
      },
      {
        path: 'work-orders',
        loadComponent: () => import('./features/work-orders/work-orders.component').then(m => m.WorkOrdersComponent)
      },
      {
        path: 'invoices',
        loadComponent: () => import('./features/invoices/invoices.component').then(m => m.InvoicesComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
