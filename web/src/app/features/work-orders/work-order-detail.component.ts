import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { WorkOrderDetail, WorkOrderStatus } from '../../core/models/api.models';
import { humanize, normalizeWorkOrderStatus, workOrderBadge, WORK_ORDER_STATUSES } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';

@Component({
  selector: 'app-work-order-detail',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './work-order-detail.component.html',
  styleUrl: '../estimates/estimate-detail.component.scss'
})
export class WorkOrderDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly working = signal(false);
  readonly order = signal<WorkOrderDetail | null>(null);
  readonly statuses = WORK_ORDER_STATUSES;

  readonly badge = workOrderBadge;
  readonly humanize = humanize;

  readonly status = computed<WorkOrderStatus | null>(() => {
    const o = this.order();
    return o ? normalizeWorkOrderStatus(o.status) : null;
  });

  readonly canInvoice = computed(() => {
    const s = this.status();
    return s === 'Completed' || s === 'Invoiced';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.fetch(id);
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getWorkOrder(id).subscribe({
      next: o => {
        this.order.set({ ...o, status: normalizeWorkOrderStatus(o.status) });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  changeStatus(status: WorkOrderStatus): void {
    const o = this.order();
    if (!o || this.working() || o.status === status) return;
    this.working.set(true);
    this.api.updateWorkOrderStatus(o.id, status).subscribe({
      next: () => {
        this.working.set(false);
        this.snack.open(`Status updated to ${humanize(status)}`, 'Dismiss', { duration: 2500 });
        this.fetch(o.id);
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not update status', 'Dismiss', { duration: 3500 });
      }
    });
  }

  createInvoice(): void {
    const o = this.order();
    if (!o || this.working()) return;
    this.working.set(true);
    this.api.createInvoiceFromWorkOrder(o.id).subscribe({
      next: invoice => {
        this.working.set(false);
        this.snack.open(`Invoice ${invoice.invoiceNumber} created`, 'View', { duration: 4000 })
          .onAction().subscribe(() => this.router.navigate(['/invoices', invoice.id]));
        this.fetch(o.id);
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not create invoice', 'Dismiss', { duration: 3500 });
      }
    });
  }

  downloadPdf(): void {
    const o = this.order();
    if (!o) return;
    this.api.workOrderPdf(o.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }
}
