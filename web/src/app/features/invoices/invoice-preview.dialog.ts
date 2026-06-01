import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/services/api.service';
import { InvoiceDetail } from '../../core/models/api.models';
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
          <div class="iv-top">
            <div class="iv-brand">
              <div class="iv-logo">A</div>
              <div>
                <div class="iv-brand-name">AutoWorks</div>
                <div class="iv-brand-sub">Workshop Manager</div>
              </div>
            </div>
            <div class="iv-doc-meta">
              <div class="iv-doc-title">INVOICE</div>
              <div class="iv-doc-num">{{ inv.invoiceNumber }}</div>
              <span class="badge" [class]="badge(inv.status)">{{ humanize(inv.status) }}</span>
            </div>
          </div>

          <div class="iv-parties">
            <div>
              <div class="iv-label">Bill To</div>
              <div class="iv-strong">{{ inv.customerName }}</div>
            </div>
            <div class="iv-dates">
              <div class="iv-label">Details</div>
              <div class="iv-row"><span>Issued</span><span>{{ inv.issuedAt | date:'mediumDate' }}</span></div>
              <div class="iv-row"><span>Due</span><span>{{ inv.dueDate ? (inv.dueDate | date:'mediumDate') : '—' }}</span></div>
              @if (inv.paidAt) {
                <div class="iv-row"><span>Paid</span><span>{{ inv.paidAt | date:'mediumDate' }}</span></div>
              }
            </div>
          </div>

          <table class="iv-table">
            <thead>
              <tr>
                <th>Description</th>
                <th class="r">Qty</th>
                <th class="r">Unit Price</th>
                <th class="r">Amount</th>
              </tr>
            </thead>
            <tbody>
              @for (line of inv.lines; track line.id) {
                <tr>
                  <td class="desc">{{ line.description }}</td>
                  <td class="r">{{ line.quantity }}</td>
                  <td class="r">{{ line.unitPrice | currency }}</td>
                  <td class="r">{{ line.lineTotal | currency }}</td>
                </tr>
              } @empty {
                <tr><td colspan="4" class="iv-empty">No line items.</td></tr>
              }
            </tbody>
          </table>

          <div class="iv-totals">
            <div class="iv-row"><span>Subtotal</span><span>{{ inv.subTotal | currency }}</span></div>
            <div class="iv-row"><span>Tax ({{ (inv.taxRate * 100) | number:'1.0-2' }}%)</span><span>{{ inv.taxAmount | currency }}</span></div>
            <div class="iv-row iv-grand"><span>Total</span><span>{{ inv.total | currency }}</span></div>
          </div>

          @if (inv.notes) {
            <div class="iv-notes">
              <div class="iv-label">Notes</div>
              <p>{{ inv.notes }}</p>
            </div>
          }
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
      color: var(--aw-text-muted);
    }
    .iv-content {
      width: 640px;
      max-width: 100%;
      max-height: 82vh;
      padding: 0;
    }
    .iv-center { display: flex; flex-direction: column; align-items: center; gap: 0.5rem; padding: 4rem 1rem; color: var(--aw-text-muted); }
    .iv-error mat-icon { font-size: 40px; width: 40px; height: 40px; opacity: 0.4; }

    .iv-doc { padding: 2.25rem 2.25rem 2rem; }

    .iv-top {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      gap: 1rem;
      padding-bottom: 1.1rem;
      border-bottom: 2px solid var(--aw-primary);
    }
    .iv-brand { display: flex; align-items: center; gap: 0.75rem; }
    .iv-logo {
      display: grid; place-items: center;
      width: 44px; height: 44px; border-radius: 11px;
      background: linear-gradient(135deg, #3b82f6, #2563eb);
      color: #fff; font-weight: 800; font-size: 1.25rem;
    }
    .iv-brand-name { font-weight: 700; font-size: 1.05rem; letter-spacing: -0.01em; }
    .iv-brand-sub { font-size: 0.75rem; color: var(--aw-text-muted); }
    .iv-doc-meta { text-align: right; }
    .iv-doc-title { font-size: 1.5rem; font-weight: 800; letter-spacing: 0.08em; color: #1e293b; }
    .iv-doc-num { font-size: 0.8rem; color: var(--aw-text-muted); margin: 2px 0 6px; }

    .iv-parties {
      display: flex;
      justify-content: space-between;
      gap: 2rem;
      margin-top: 1.5rem;
    }
    .iv-label {
      font-size: 0.68rem; font-weight: 700; letter-spacing: 0.06em;
      text-transform: uppercase; color: #94a3b8; margin-bottom: 0.35rem;
    }
    .iv-strong { font-weight: 700; font-size: 0.95rem; }
    .iv-dates { min-width: 220px; }
    .iv-row {
      display: flex; justify-content: space-between; gap: 1.5rem;
      font-size: 0.85rem; padding: 2px 0;
    }
    .iv-row > span:first-child { color: var(--aw-text-muted); }

    .iv-table {
      width: 100%;
      border-collapse: collapse;
      margin-top: 1.75rem;
    }
    .iv-table thead th {
      text-align: left;
      font-size: 0.66rem; font-weight: 700; letter-spacing: 0.05em; text-transform: uppercase;
      color: #94a3b8;
      background: #f8fafc;
      padding: 0.6rem 0.75rem;
      border-bottom: 1px solid var(--aw-border);
    }
    .iv-table th.r, .iv-table td.r { text-align: right; }
    .iv-table tbody td {
      padding: 0.7rem 0.75rem;
      font-size: 0.875rem;
      border-bottom: 1px solid #f1f5f9;
    }
    .iv-table td.desc { font-weight: 600; }
    .iv-empty { text-align: center; color: var(--aw-text-muted); padding: 1.5rem !important; }

    .iv-totals {
      margin-top: 1rem;
      margin-left: auto;
      width: 280px;
    }
    .iv-totals .iv-row { padding: 0.3rem 0.75rem; }
    .iv-grand {
      border-top: 2px solid var(--aw-border);
      margin-top: 0.35rem;
      padding-top: 0.6rem !important;
      font-size: 1.05rem; font-weight: 800;
    }
    .iv-grand > span:first-child { color: var(--aw-text) !important; }
    .iv-grand > span:last-child { color: var(--aw-primary); }

    .iv-notes {
      margin-top: 1.5rem;
      padding: 0.9rem 1.1rem;
      background: #f0f7ff;
      border: 1px solid #dbeafe;
      border-radius: var(--aw-radius-sm);
    }
    .iv-notes p { margin: 0; font-size: 0.85rem; color: #334155; }

    @media (max-width: 600px) {
      .iv-doc { padding: 2rem 1.1rem 1.5rem; }
      .iv-parties { flex-direction: column; gap: 1rem; }
      .iv-dates { min-width: 0; }
      .iv-totals { width: 100%; }
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
}
