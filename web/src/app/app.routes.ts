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
        path: 'estimates',
        loadComponent: () => import('./features/estimates/estimates.component').then(m => m.EstimatesComponent)
      },
      {
        path: 'estimates/:id',
        loadComponent: () => import('./features/estimates/estimate-detail.component').then(m => m.EstimateDetailComponent)
      },
      {
        path: 'work-orders',
        loadComponent: () => import('./features/work-orders/work-orders.component').then(m => m.WorkOrdersComponent)
      },
      {
        path: 'work-orders/:id',
        loadComponent: () =>
          import('./features/work-orders/work-order-detail.component').then(m => m.WorkOrderDetailComponent)
      },
      {
        path: 'invoices',
        loadComponent: () => import('./features/invoices/invoices.component').then(m => m.InvoicesComponent)
      },
      {
        path: 'invoices/:id',
        loadComponent: () =>
          import('./features/invoices/invoice-detail.component').then(m => m.InvoiceDetailComponent)
      },
      {
        path: 'scheduler',
        loadComponent: () => import('./features/scheduler/scheduler.component').then(m => m.SchedulerComponent)
      },
      {
        path: 'parts',
        loadComponent: () => import('./features/parts/parts.component').then(m => m.PartsComponent)
      },
      {
        path: 'reports',
        loadComponent: () => import('./features/reports/reports.component').then(m => m.ReportsComponent)
      }
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
