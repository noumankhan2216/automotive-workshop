import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { ApiService } from '../../core/services/api.service';
import { WorkOrder } from '../../core/models/api.models';
import { normalizeWorkOrderStatus } from '../../core/utils/status.util';

@Component({
  selector: 'app-invoice-create-dialog',
  standalone: true,
  imports: [
    CurrencyPipe,
    FormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatSelectModule,
    MatButtonModule,
    MatProgressSpinnerModule
  ],
  template: `
    <h2 mat-dialog-title>Create Invoice</h2>
    <mat-dialog-content>
      @if (loading()) {
        <div class="center"><mat-spinner diameter="36" /></div>
      } @else if (eligible().length === 0) {
        <p class="muted">
          No completed work orders are available to invoice. Mark a work order as
          <strong>Completed</strong> first.
        </p>
      } @else {
        <p class="muted">Select a completed work order to generate an invoice.</p>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Work Order</mat-label>
          <mat-select [(ngModel)]="selectedId">
            @for (wo of eligible(); track wo.id) {
              <mat-option [value]="wo.id">
                {{ wo.workOrderNumber }} — {{ wo.customerName }} ({{ wo.totalAmount | currency }})
              </mat-option>
            }
          </mat-select>
        </mat-form-field>
      }
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button type="button" mat-dialog-close>Cancel</button>
      <button mat-flat-button color="primary" [disabled]="!selectedId" (click)="confirm()">
        Generate Invoice
      </button>
    </mat-dialog-actions>
  `,
  styles: [`
    mat-dialog-content { min-width: 440px; }
    .full { width: 100%; }
    .muted { color: var(--aw-text-muted); font-size: 0.9rem; }
    .center { display: flex; justify-content: center; padding: 1.5rem; }
    @media (max-width: 520px) { mat-dialog-content { min-width: unset; } }
  `]
})
export class InvoiceCreateDialog implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<InvoiceCreateDialog>);

  readonly loading = signal(true);
  readonly workOrders = signal<WorkOrder[]>([]);
  selectedId = '';

  readonly eligible = computed(() =>
    this.workOrders().filter(w => normalizeWorkOrderStatus(w.status) === 'Completed')
  );

  ngOnInit(): void {
    this.api.getWorkOrders('Completed', 1, 200).subscribe({
      next: res => {
        this.workOrders.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  confirm(): void {
    if (this.selectedId) this.dialogRef.close(this.selectedId);
  }
}
