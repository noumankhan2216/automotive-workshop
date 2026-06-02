import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { InvoiceDetail, InvoiceLine, InvoiceStatus } from '../../core/models/api.models';
import { humanize, invoiceBadge, INVOICE_STATUSES, normalizeInvoiceStatus } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';

@Component({
  selector: 'app-invoice-detail',
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
  templateUrl: './invoice-detail.component.html',
  styleUrl: '../estimates/estimate-detail.component.scss'
})
export class InvoiceDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly working = signal(false);
  readonly invoice = signal<InvoiceDetail | null>(null);
  readonly statuses = INVOICE_STATUSES;

  readonly badge = invoiceBadge;
  readonly humanize = humanize;

  readonly status = computed<InvoiceStatus | null>(() => {
    const inv = this.invoice();
    return inv ? normalizeInvoiceStatus(inv.status) : null;
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.fetch(id);
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getInvoice(id).subscribe({
      next: inv => {
        this.invoice.set({ ...inv, status: normalizeInvoiceStatus(inv.status) });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  changeStatus(status: InvoiceStatus): void {
    const inv = this.invoice();
    if (!inv || this.working() || inv.status === status) return;
    this.working.set(true);
    this.api.updateInvoiceStatus(inv.id, status).subscribe({
      next: () => {
        this.working.set(false);
        this.snack.open(`Invoice marked ${humanize(status)}`, 'Dismiss', { duration: 2500 });
        this.fetch(inv.id);
      },
      error: () => {
        this.working.set(false);
        this.snack.open('Could not update invoice', 'Dismiss', { duration: 3000 });
      }
    });
  }

  balanceDue(inv: InvoiceDetail): number {
    return inv.status === 'Paid' ? 0 : inv.total;
  }

  lineTax(line: InvoiceLine, inv: InvoiceDetail): number {
    return Math.round(line.lineTotal * inv.taxRate * 100) / 100;
  }

  terms(inv: InvoiceDetail): string {
    if (!inv.dueDate) return 'Due on receipt';
    const days = Math.round(
      (new Date(inv.dueDate).getTime() - new Date(inv.issuedAt).getTime()) / 86_400_000
    );
    return days > 0 ? `Net ${days}` : 'Due on receipt';
  }

  downloadPdf(): void {
    const inv = this.invoice();
    if (!inv) return;
    this.api.invoicePdf(inv.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }
}
