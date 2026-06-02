import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Estimate, EstimateStatus } from '../../core/models/api.models';
import { estimateBadge, ESTIMATE_STATUSES, humanize, normalizeEstimateStatus } from '../../core/utils/status.util';
import { openPdfBlob } from '../../core/utils/pdf.util';
import { EstimateFormDialog, EstimateFormResult } from './estimate-form.dialog';

@Component({
  selector: 'app-estimates',
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
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './estimates.component.html',
  styleUrl: './estimates.component.scss'
})
export class EstimatesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly router = inject(Router);

  readonly loading = signal(true);
  readonly estimates = signal<Estimate[]>([]);
  readonly statuses = ESTIMATE_STATUSES;
  statusFilter: EstimateStatus | '' = '';
  readonly displayedColumns = ['number', 'customer', 'vehicle', 'status', 'total', 'validUntil', 'actions'];

  readonly badge = estimateBadge;
  readonly humanize = humanize;

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getEstimates(this.statusFilter || undefined).subscribe({
      next: result => {
        this.estimates.set(
          result.items.map(e => ({ ...e, status: normalizeEstimateStatus(e.status) }))
        );
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  open(estimate: Estimate): void {
    this.router.navigate(['/estimates', estimate.id]);
  }

  create(): void {
    this.dialog
      .open(EstimateFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable' })
      .afterClosed()
      .subscribe((result: EstimateFormResult | undefined) => {
        if (!result) return;
        this.api.createEstimate(result.payload).subscribe({
          next: created => {
            this.snack.open('Estimate created', 'Dismiss', { duration: 2500 });
            this.router.navigate(['/estimates', created.id]);
          },
          error: () => this.snack.open('Could not create estimate', 'Dismiss', { duration: 3000 })
        });
      });
  }

  downloadPdf(estimate: Estimate, event: MouseEvent): void {
    event.stopPropagation();
    this.api.estimatePdf(estimate.id).subscribe({
      next: blob => openPdfBlob(blob),
      error: () => this.snack.open('Could not generate PDF', 'Dismiss', { duration: 3000 })
    });
  }
}
