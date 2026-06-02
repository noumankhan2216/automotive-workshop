import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe, PercentPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ApiService } from '../../core/services/api.service';
import { SalesReport, TaxReport, TechnicianProductivityReport } from '../../core/models/api.models';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    PercentPipe,
    FormsModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatProgressSpinnerModule,
    MatTableModule
  ],
  templateUrl: './reports.component.html',
  styleUrl: './reports.component.scss'
})
export class ReportsComponent implements OnInit {
  private readonly api = inject(ApiService);

  readonly loading = signal(false);
  fromDate = this.isoDateDaysAgo(30);
  toDate = this.isoDateToday();

  readonly sales = signal<SalesReport | null>(null);
  readonly tax = signal<TaxReport | null>(null);
  readonly productivity = signal<TechnicianProductivityReport | null>(null);

  readonly salesColumns = ['date', 'invoiceCount', 'subTotal', 'taxAmount', 'total'];
  readonly taxColumns = ['date', 'taxableAmount', 'taxAmount'];
  readonly techColumns = ['userName', 'totalHours', 'jobsAssigned', 'jobsCompleted', 'openTimeEntries'];

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    const from = this.toIsoStart(this.fromDate);
    const to = this.toIsoEnd(this.toDate);
    this.loading.set(true);

    let pending = 3;
    const done = () => {
      pending--;
      if (pending === 0) this.loading.set(false);
    };

    this.api.getSalesReport(from, to).subscribe({
      next: r => { this.sales.set(r); done(); },
      error: () => done()
    });
    this.api.getTaxReport(from, to).subscribe({
      next: r => { this.tax.set(r); done(); },
      error: () => done()
    });
    this.api.getTechnicianProductivityReport(from, to).subscribe({
      next: r => { this.productivity.set(r); done(); },
      error: () => done()
    });
  }

  private isoDateDaysAgo(days: number): string {
    const d = new Date();
    d.setDate(d.getDate() - days);
    return d.toISOString().slice(0, 10);
  }

  private isoDateToday(): string {
    return new Date().toISOString().slice(0, 10);
  }

  private toIsoStart(date: string): string {
    return new Date(date + 'T00:00:00').toISOString();
  }

  private toIsoEnd(date: string): string {
    const d = new Date(date + 'T00:00:00');
    d.setDate(d.getDate() + 1);
    return d.toISOString();
  }
}
