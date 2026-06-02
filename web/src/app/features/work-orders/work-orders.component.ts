import { Component, inject, OnInit, signal } from '@angular/core';
import { Router } from '@angular/router';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { CreateWorkOrderRequest, WorkOrder, WorkOrderStatus } from '../../core/models/api.models';
import { humanize, normalizeWorkOrderStatus, workOrderBadge, WORK_ORDER_STATUSES } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';
import { WorkOrderFormDialog } from './work-order-form.dialog';

@Component({
  selector: 'app-work-orders',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    FormsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatMenuModule,
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './work-orders.component.html',
  styleUrl: './work-orders.component.scss'
})
export class WorkOrdersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly workOrders = signal<WorkOrder[]>([]);
  readonly statuses = WORK_ORDER_STATUSES;
  statusFilter: WorkOrderStatus | '' = '';
  readonly displayedColumns = ['number', 'customer', 'vehicle', 'status', 'total', 'openedAt', 'actions'];

  readonly badge = workOrderBadge;
  readonly humanize = humanize;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getWorkOrders(this.statusFilter || undefined).subscribe({
      next: result => {
        this.workOrders.set(
          result.items.map(w => ({ ...w, status: normalizeWorkOrderStatus(w.status) }))
        );
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  open(order: WorkOrder): void {
    this.router.navigate(['/work-orders', order.id]);
  }

  create(): void {
    this.dialog
      .open(WorkOrderFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable' })
      .afterClosed()
      .subscribe((payload: CreateWorkOrderRequest | undefined) => {
        if (!payload) return;
        this.api.createWorkOrder(payload).subscribe({
          next: () => {
            this.snack.open('Work order created', 'Dismiss', { duration: 2500 });
            this.load();
          },
          error: () => this.snack.open('Could not create work order', 'Dismiss', { duration: 3000 })
        });
      });
  }

  downloadPdf(order: WorkOrder): void {
    this.api.workOrderPdf(order.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }

  changeStatus(order: WorkOrder, status: WorkOrderStatus): void {
    if (order.status === status) return;
    this.api.updateWorkOrderStatus(order.id, status).subscribe({
      next: () => {
        this.snack.open(`Status updated to ${humanize(status)}`, 'Dismiss', { duration: 2500 });
        this.load();
      },
      error: () => this.snack.open('Could not update status', 'Dismiss', { duration: 3000 })
    });
  }
}
