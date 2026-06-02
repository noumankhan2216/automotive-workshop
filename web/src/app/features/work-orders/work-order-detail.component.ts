import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { TechnicianUser, WorkOrderDetail, WorkOrderStatus } from '../../core/models/api.models';
import { humanize, normalizeWorkOrderStatus, workOrderBadge, WORK_ORDER_STATUSES } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';

@Component({
  selector: 'app-work-order-detail',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatFormFieldModule
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
  readonly technicians = signal<TechnicianUser[]>([]);
  readonly statuses = WORK_ORDER_STATUSES;
  selectedTechId: string | null = null;

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

  readonly hasPartLines = computed(() => {
    const o = this.order();
    return o ? o.items.some(i => i.partId && !i.partsIssued) : false;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.fetch(id);
    this.api.getTechnicians().subscribe({
      next: t => this.technicians.set(t)
    });
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getWorkOrder(id).subscribe({
      next: o => {
        const normalized = {
          ...o,
          status: normalizeWorkOrderStatus(o.status),
          timeEntries: o.timeEntries ?? [],
          items: (o.items ?? []).map(i => ({ ...i, partsIssued: i.partsIssued ?? false }))
        };
        this.order.set(normalized);
        this.selectedTechId = o.assignedToUserId ?? null;
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

  assignTechnician(): void {
    const o = this.order();
    if (!o || this.working()) return;
    this.working.set(true);
    this.api.assignWorkOrder(o.id, { assignedToUserId: this.selectedTechId ?? undefined }).subscribe({
      next: () => {
        this.working.set(false);
        this.snack.open('Technician assigned', 'Dismiss', { duration: 2500 });
        this.fetch(o.id);
      },
      error: () => {
        this.working.set(false);
        this.snack.open('Could not assign technician', 'Dismiss', { duration: 3000 });
      }
    });
  }

  clockIn(): void {
    const o = this.order();
    if (!o || this.working()) return;
    this.working.set(true);
    this.api.clockIn(o.id, { userId: this.selectedTechId ?? undefined }).subscribe({
      next: () => {
        this.working.set(false);
        this.snack.open('Clocked in', 'Dismiss', { duration: 2500 });
        this.fetch(o.id);
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not clock in', 'Dismiss', { duration: 3500 });
      }
    });
  }

  clockOut(entryId: string): void {
    const o = this.order();
    if (!o || this.working()) return;
    this.working.set(true);
    this.api.clockOut(entryId).subscribe({
      next: () => {
        this.working.set(false);
        this.snack.open('Clocked out', 'Dismiss', { duration: 2500 });
        this.fetch(o.id);
      },
      error: () => {
        this.working.set(false);
        this.snack.open('Could not clock out', 'Dismiss', { duration: 3000 });
      }
    });
  }

  issueParts(): void {
    const o = this.order();
    if (!o || this.working()) return;
    this.working.set(true);
    this.api.issueWorkOrderParts(o.id).subscribe({
      next: result => {
        this.working.set(false);
        const msg = result.messages.join(' · ') || 'Parts issued';
        this.snack.open(msg, 'Dismiss', { duration: 4500 });
        this.fetch(o.id);
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not issue parts', 'Dismiss', { duration: 3500 });
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
