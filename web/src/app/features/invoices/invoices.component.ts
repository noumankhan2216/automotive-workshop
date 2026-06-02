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
import { Invoice, InvoiceStatus } from '../../core/models/api.models';
import { humanize, invoiceBadge, normalizeInvoiceStatus, INVOICE_STATUSES } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';
import { InvoiceCreateDialog } from './invoice-create.dialog';

@Component({
  selector: 'app-invoices',
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
  templateUrl: './invoices.component.html',
  styleUrl: './invoices.component.scss'
})
export class InvoicesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly invoices = signal<Invoice[]>([]);
  readonly statuses = INVOICE_STATUSES;
  statusFilter: InvoiceStatus | '' = '';
  readonly displayedColumns = ['number', 'customer', 'status', 'total', 'issuedAt', 'dueDate', 'actions'];

  readonly badge = invoiceBadge;
  readonly humanize = humanize;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getInvoices(this.statusFilter || undefined).subscribe({
      next: result => {
        this.invoices.set(
          result.items.map(i => ({ ...i, status: normalizeInvoiceStatus(i.status) }))
        );
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  create(): void {
    this.dialog
      .open(InvoiceCreateDialog, { panelClass: 'aw-dialog' })
      .afterClosed()
      .subscribe((workOrderId: string | undefined) => {
        if (!workOrderId) return;
        this.api.createInvoiceFromWorkOrder(workOrderId).subscribe({
          next: () => {
            this.snack.open('Invoice generated', 'Dismiss', { duration: 2500 });
            this.load();
          },
          error: err =>
            this.snack.open(err?.error?.message ?? 'Could not create invoice', 'Dismiss', { duration: 3500 })
        });
      });
  }

  open(invoice: Invoice): void {
    this.router.navigate(['/invoices', invoice.id]);
  }

  downloadPdf(invoice: Invoice): void {
    this.api.invoicePdf(invoice.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }

  changeStatus(invoice: Invoice, status: InvoiceStatus): void {
    if (invoice.status === status) return;
    this.api.updateInvoiceStatus(invoice.id, status).subscribe({
      next: () => {
        this.snack.open(`Invoice marked ${humanize(status)}`, 'Dismiss', { duration: 2500 });
        this.load();
      },
      error: () => this.snack.open('Could not update invoice', 'Dismiss', { duration: 3000 })
    });
  }
}
