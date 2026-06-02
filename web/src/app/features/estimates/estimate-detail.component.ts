import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { EstimateDetail, EstimateStatus } from '../../core/models/api.models';
import { estimateBadge, humanize, normalizeEstimateStatus } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';
import { EstimateFormDialog, EstimateFormResult } from './estimate-form.dialog';

@Component({
  selector: 'app-estimate-detail',
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
    MatProgressSpinnerModule,
    MatDialogModule
  ],
  templateUrl: './estimate-detail.component.html',
  styleUrl: './estimate-detail.component.scss'
})
export class EstimateDetailComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly working = signal(false);
  readonly estimate = signal<EstimateDetail | null>(null);

  readonly badge = estimateBadge;
  readonly humanize = humanize;

  readonly status = computed<EstimateStatus | null>(() => {
    const e = this.estimate();
    return e ? normalizeEstimateStatus(e.status) : null;
  });

  readonly canEdit = computed(() => {
    const s = this.status();
    return s !== null && s !== 'Converted';
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.fetch(id);
  }

  private fetch(id: string): void {
    this.loading.set(true);
    this.api.getEstimate(id).subscribe({
      next: e => {
        this.estimate.set({ ...e, status: normalizeEstimateStatus(e.status) });
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  setStatus(status: EstimateStatus): void {
    const e = this.estimate();
    if (!e || this.working()) return;
    this.working.set(true);
    this.api.updateEstimateStatus(e.id, status).subscribe({
      next: updated => {
        this.estimate.set({ ...updated, status: normalizeEstimateStatus(updated.status) });
        this.working.set(false);
        this.snack.open(`Estimate marked ${humanize(status)}`, 'Dismiss', { duration: 2500 });
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not update status', 'Dismiss', { duration: 3500 });
      }
    });
  }

  edit(): void {
    const e = this.estimate();
    if (!e) return;
    this.dialog
      .open(EstimateFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable', data: { estimate: e } })
      .afterClosed()
      .subscribe((result: EstimateFormResult | undefined) => {
        if (!result?.id) return;
        this.api.updateEstimate(result.id, {
          customerNotes: result.payload.customerNotes,
          validUntil: result.payload.validUntil,
          items: result.payload.items
        }).subscribe({
          next: updated => {
            this.estimate.set({ ...updated, status: normalizeEstimateStatus(updated.status) });
            this.snack.open('Estimate updated', 'Dismiss', { duration: 2500 });
          },
          error: err => this.snack.open(err?.error?.message ?? 'Could not update estimate', 'Dismiss', { duration: 3500 })
        });
      });
  }

  convert(): void {
    const e = this.estimate();
    if (!e || this.working()) return;
    this.working.set(true);
    this.api.convertEstimate(e.id).subscribe({
      next: workOrder => {
        this.working.set(false);
        this.snack.open(`Converted to ${workOrder.workOrderNumber}`, 'View', { duration: 4000 })
          .onAction().subscribe(() => this.router.navigate(['/work-orders', workOrder.id]));
        this.fetch(e.id);
      },
      error: err => {
        this.working.set(false);
        this.snack.open(err?.error?.message ?? 'Could not convert estimate', 'Dismiss', { duration: 3500 });
      }
    });
  }

  downloadPdf(): void {
    const e = this.estimate();
    if (!e) return;
    this.api.estimatePdf(e.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }

  remove(): void {
    const e = this.estimate();
    if (!e) return;
    if (!confirm(`Delete estimate ${e.estimateNumber}?`)) return;
    this.api.deleteEstimate(e.id).subscribe({
      next: () => {
        this.snack.open('Estimate deleted', 'Dismiss', { duration: 2500 });
        this.router.navigate(['/estimates']);
      },
      error: err => this.snack.open(err?.error?.message ?? 'Could not delete estimate', 'Dismiss', { duration: 3500 })
    });
  }

  lineTax(lineTotal: number, taxRate: number): number {
    return Math.round(lineTotal * taxRate * 100) / 100;
  }
}
