import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../core/services/api.service';
import { DashboardSummary } from '../../core/models/api.models';

interface Kpi {
  label: string;
  value: string;
  hint?: string;
  icon: string;
  tone: string;
  route?: string;
  queryParams?: Record<string, string>;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [RouterLink, MatProgressSpinnerModule, MatIconModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly currency = new CurrencyPipe('en-US');

  readonly loading = signal(true);
  readonly summary = signal<DashboardSummary | null>(null);

  private money(value: number): string {
    return this.currency.transform(value, 'USD', 'symbol', '1.0-0') ?? `$${value}`;
  }

  readonly revenueCards = computed<Kpi[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      { label: 'Revenue Today', value: this.money(s.revenueToday), icon: 'today', tone: 'tone-blue' },
      { label: 'Revenue This Week', value: this.money(s.revenueThisWeek), icon: 'date_range', tone: 'tone-indigo' },
      { label: 'Revenue This Month', value: this.money(s.revenueThisMonth), icon: 'calendar_month', tone: 'tone-green' }
    ];
  });

  readonly opsCards = computed<Kpi[]>(() => {
    const s = this.summary();
    if (!s) return [];
    return [
      {
        label: 'Open Work Orders',
        value: `${s.openWorkOrders}`,
        hint: 'View active jobs',
        icon: 'build',
        tone: 'tone-amber',
        route: '/work-orders'
      },
      {
        label: 'Completed This Month',
        value: `${s.completedWorkOrdersThisMonth}`,
        hint: 'Jobs finished',
        icon: 'task_alt',
        tone: 'tone-teal',
        route: '/work-orders'
      },
      {
        label: 'Outstanding Invoices',
        value: `${s.outstandingInvoices}`,
        hint: `${this.money(s.outstandingAmount)} unpaid`,
        icon: 'receipt_long',
        tone: 'tone-red',
        route: '/invoices'
      },
      {
        label: 'Low Stock Parts',
        value: `${s.lowStockParts}`,
        hint: s.lowStockParts > 0 ? 'Needs reorder' : 'Stock levels OK',
        icon: 'inventory_2',
        tone: s.lowStockParts > 0 ? 'tone-red' : 'tone-teal',
        route: '/parts',
        queryParams: s.lowStockParts > 0 ? { lowStock: '1' } : undefined
      }
    ];
  });

  ngOnInit(): void {
    this.api.getDashboardSummary().subscribe({
      next: data => {
        this.summary.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
