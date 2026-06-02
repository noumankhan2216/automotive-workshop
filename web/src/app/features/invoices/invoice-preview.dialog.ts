import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/services/api.service';
import { InvoiceDetail, InvoiceLine } from '../../core/models/api.models';
import { humanize, invoiceBadge, normalizeInvoiceStatus } from '../../core/utils/status.util';

export interface InvoicePreviewData {
  id: string;
}

@Component({
  selector: 'app-invoice-preview-dialog',
  standalone: true,
  imports: [
    CurrencyPipe,
    DatePipe,
    DecimalPipe,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  template: `
    <button mat-icon-button class="iv-close" aria-label="Close preview" mat-dialog-close>
      <mat-icon>close</mat-icon>
    </button>

    <mat-dialog-content class="iv-content">
      @if (loading()) {
        <div class="iv-center"><mat-spinner diameter="40" /></div>
      } @else {
        @if (invoice(); as inv) {
        <div class="iv-doc">
          <!-- Header -->
          <header class="iv-head">
            <div class="iv-brand">
              <div class="iv-logo">A</div>
              <span class="iv-wordmark">AutoWorks</span>
            </div>
            <div class="iv-head-right">
              <div class="iv-title">INVOICE</div>
              <div class="iv-invno">Invoice# {{ inv.invoiceNumber }}</div>
            </div>
          </header>

          <!-- Company + balance due -->
          <section class="iv-meta">
            <address class="iv-company">
              AutoWorks Workshop SMC Private Limited<br />
              NTN: G463350-3<br />
              100 Garage Lane, Gulberg 3<br />
              Lahore, Punjab 54000<br />
              Pakistan
            </address>
            <div class="iv-balance">
              <div class="iv-balance-label">Balance Due</div>
              <div class="iv-balance-amt">{{ balanceDue(inv) | currency }}</div>
            </div>
          </section>

          <!-- Bill to + dates -->
          <section class="iv-parties">
            <div class="iv-billto">
              <div class="iv-label">Bill To</div>
              <div class="iv-strong">{{ inv.customerName }}</div>
            </div>
            <div class="iv-dates">
              <div class="iv-drow"><span class="iv-dlabel">Invoice Date :</span><span>{{ inv.issuedAt | date:'dd MMM yyyy' }}</span></div>
              <div class="iv-drow"><span class="iv-dlabel">Terms :</span><span>{{ terms(inv) }}</span></div>
              <div class="iv-drow"><span class="iv-dlabel">Due Date :</span><span>{{ inv.dueDate ? (inv.dueDate | date:'dd MMM yyyy') : '—' }}</span></div>
            </div>
          </section>

          <!-- Line items -->
          <table class="iv-table">
            <thead>
              <tr>
                <th class="c-num">#</th>
                <th>Item &amp; Description</th>
                <th class="r">Qty</th>
                <th class="r">Tax</th>
                <th class="r">Amount</th>
              </tr>
            </thead>
            <tbody>
              @for (line of inv.lines; track line.id; let i = $index) {
                <tr>
                  <td class="c-num">{{ i + 1 }}</td>
                  <td class="desc">{{ line.description }}</td>
                  <td class="r">{{ line.quantity | number:'1.0-2' }}</td>
                  <td class="r">{{ lineTax(line, inv) | number:'1.2-2' }}</td>
                  <td class="r">{{ line.lineTotal | number:'1.2-2' }}</td>
                </tr>
              } @empty {
                <tr><td colspan="5" class="iv-empty">No line items.</td></tr>
              }
            </tbody>
          </table>

          <!-- Totals -->
          <section class="iv-totals">
            <div class="iv-trow"><span>Sub Total</span><span>{{ inv.subTotal | number:'1.2-2' }}</span></div>
            <div class="iv-trow"><span>Sales Tax ({{ (inv.taxRate * 100) | number:'1.0-2' }}%)</span><span>{{ inv.taxAmount | number:'1.2-2' }}</span></div>
            <div class="iv-trow iv-total"><span>Total</span><span>{{ inv.total | currency }}</span></div>
            <div class="iv-trow iv-balrow"><span>Balance Due</span><span>{{ balanceDue(inv) | currency }}</span></div>
          </section>

          <!-- Payment + notes -->
          <footer class="iv-foot">
            <div class="iv-pay">
              <div class="iv-label">Payment Details</div>
              <div>Account Title: AutoWorks Workshop (SMC-Private) Limited</div>
              <div>Bank Name: Meezan Bank Limited</div>
              <div>Account Number: 1160 0112 1802 06</div>
              <div>IBAN: PK88 MEZN 0011 6001 1218 0206</div>
            </div>
            @if (inv.notes) {
              <div class="iv-notes">
                <div class="iv-label">Notes</div>
                <p>{{ inv.notes }}</p>
              </div>
            }
          </footer>
        </div>
        } @else {
          <div class="iv-center iv-error">
            <mat-icon>error_outline</mat-icon>
            <p>Could not load this invoice.</p>
          </div>
        }
      }
    </mat-dialog-content>
  `,
  styles: [`
    :host { display: block; }
    .iv-close {
      position: absolute;
      top: 8px;
      right: 8px;
      z-index: 2;
      background: rgba(255, 255, 255, 0.9);
      color: var(--aw-text-muted);
    }
    .iv-content {
      width: 720px;
      max-width: 100%;
      max-height: 85vh;
      padding: 0;
      color: #1f2937;
    }
    .iv-center { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 4rem 1rem; color: var(--aw-text-muted); }
    .iv-error mat-icon { font-size: 40px; width: 40px; height: 40px; opacity: 0.4; }

    .iv-doc { padding: 2.5rem 2.5rem 2rem; font-size: 0.85rem; line-height: 1.5; }

    /* Header */
    .iv-head {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
    }
    .iv-brand { display: flex; align-items: center; gap: 0.6rem; }
    .iv-logo {
      display: grid; place-items: center;
      width: 40px; height: 40px; border-radius: 10px;
      background: linear-gradient(135deg, #3b82f6, #2563eb);
      color: #fff; font-weight: 800; font-size: 1.15rem;
    }
    .iv-wordmark { font-size: 1.7rem; font-weight: 800; letter-spacing: -0.03em; color: #111827; }
    .iv-head-right { text-align: right; }
    .iv-title { font-size: 2.1rem; font-weight: 300; letter-spacing: 0.04em; color: #4b5563; line-height: 1; }
    .iv-invno { font-size: 0.82rem; font-weight: 700; color: #111827; margin-top: 0.4rem; }

    /* Company + balance */
    .iv-meta {
      display: flex;
      justify-content: space-between;
      gap: 1.5rem;
      margin-top: 1.75rem;
    }
    .iv-company { font-style: normal; color: #4b5563; font-size: 0.8rem; line-height: 1.6; }
    .iv-balance { text-align: right; min-width: 180px; }
    .iv-balance-label { font-size: 0.8rem; font-weight: 700; color: #4b5563; }
    .iv-balance-amt { font-size: 1.05rem; font-weight: 800; color: #111827; margin-top: 0.15rem; }

    /* Bill to + dates */
    .iv-parties {
      display: flex;
      justify-content: space-between;
      gap: 2rem;
      margin-top: 2rem;
    }
    .iv-label { font-size: 0.8rem; font-weight: 600; color: #6b7280; margin-bottom: 0.3rem; }
    .iv-strong { font-weight: 700; font-size: 0.95rem; color: #111827; }
    .iv-billto .iv-strong + * { color: #4b5563; }
    .iv-dates { min-width: 260px; }
    .iv-drow { display: flex; justify-content: space-between; gap: 1.5rem; padding: 3px 0; }
    .iv-dlabel { color: #6b7280; }

    /* Table */
    .iv-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 1.75rem;
    }
    .iv-table thead th {
      background: #374151;
      color: #fff;
      font-weight: 600;
      font-size: 0.8rem;
      text-align: left;
      padding: 0.6rem 0.8rem;
    }
    .iv-table th.c-num, .iv-table td.c-num { width: 36px; text-align: left; color: #6b7280; }
    .iv-table th.r, .iv-table td.r { text-align: right; }
    .iv-table tbody td {
      padding: 0.75rem 0.8rem;
      border-bottom: 1px solid #e5e7eb;
      vertical-align: top;
    }
    .iv-table td.desc { color: #111827; font-weight: 600; }
    .iv-empty { text-align: center !important; color: var(--aw-text-muted); padding: 1.5rem !important; }

    /* Totals */
    .iv-totals {
      margin-top: 1.25rem;
      margin-left: auto;
      width: 320px;
    }
    .iv-trow { display: flex; justify-content: space-between; gap: 1.5rem; padding: 0.45rem 0.8rem; }
    .iv-trow > span:first-child { color: #4b5563; }
    .iv-total { font-weight: 800; color: #111827; }
    .iv-total > span:first-child { color: #111827; }
    .iv-balrow {
      background: #f3f4f6;
      font-weight: 800;
      color: #111827;
      border-radius: 4px;
    }
    .iv-balrow > span:first-child { color: #111827; }

    /* Footer */
    .iv-foot {
      display: flex;
      justify-content: space-between;
      gap: 2rem;
      margin-top: 2.5rem;
      padding-top: 1.25rem;
      border-top: 1px solid #e5e7eb;
      font-size: 0.78rem;
      color: #4b5563;
      line-height: 1.7;
    }
    .iv-notes { max-width: 260px; }
    .iv-notes p { margin: 0; }

    @media (max-width: 640px) {
      .iv-doc { padding: 2rem 1.1rem 1.5rem; }
      .iv-head { flex-direction: column; gap: 0.75rem; }
      .iv-head-right { text-align: left; }
      .iv-meta, .iv-parties, .iv-foot { flex-direction: column; gap: 1rem; }
      .iv-balance { text-align: left; min-width: 0; }
      .iv-dates { min-width: 0; }
      .iv-totals { width: 100%; }
      .iv-table { font-size: 0.78rem; }
    }
  `]
})
export class InvoicePreviewDialog implements OnInit {
  private readonly api = inject(ApiService);
  private readonly data = inject<InvoicePreviewData>(MAT_DIALOG_DATA);
  readonly dialogRef = inject(MatDialogRef<InvoicePreviewDialog>);

  readonly loading = signal(true);
  readonly invoice = signal<InvoiceDetail | null>(null);

  readonly badge = invoiceBadge;
  readonly humanize = humanize;

  ngOnInit(): void {
    this.api.getInvoice(this.data.id).subscribe({
      next: inv => {
        this.invoice.set({ ...inv, status: normalizeInvoiceStatus(inv.status) });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  /** Outstanding balance: zero once the invoice has been paid. */
  balanceDue(inv: InvoiceDetail): number {
    return inv.status === 'Paid' ? 0 : inv.total;
  }

  /** Per-line tax derived from the invoice tax rate. */
  lineTax(line: InvoiceLine, inv: InvoiceDetail): number {
    return Math.round(line.lineTotal * inv.taxRate * 100) / 100;
  }

  /** Payment terms label, e.g. "Net 14". */
  terms(inv: InvoiceDetail): string {
    if (!inv.dueDate) return 'Due on receipt';
    const days = Math.round(
      (new Date(inv.dueDate).getTime() - new Date(inv.issuedAt).getTime()) / 86_400_000
    );
    return days > 0 ? `Net ${days}` : 'Due on receipt';
  }
}
