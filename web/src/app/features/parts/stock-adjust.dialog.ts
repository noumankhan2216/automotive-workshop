import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { AdjustPartStockRequest, Part, PartStockTransactionType } from '../../core/models/api.models';

@Component({
  selector: 'app-stock-adjust-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, MatButtonModule],
  template: `
    <h2 mat-dialog-title>Adjust Stock — {{ data.part.name }}</h2>
    <p class="hint">On hand: <strong>{{ data.part.quantityOnHand }}</strong></p>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Type</mat-label>
          <mat-select formControlName="type" required>
            <mat-option value="Receive">Receive (+)</mat-option>
            <mat-option value="Issue">Issue (−)</mat-option>
            <mat-option value="Adjustment">Adjustment (+/−)</mat-option>
            <mat-option value="Return">Return (+)</mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Quantity change</mat-label>
          <input matInput type="number" formControlName="quantityChange" required />
          <mat-hint>Use negative values for reductions</mat-hint>
        </mat-form-field>
        <mat-form-field appearance="outline" class="full">
          <mat-label>Notes</mat-label>
          <input matInput formControlName="notes" />
        </mat-form-field>
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Apply</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`.full { width: 100%; } .hint { margin: 0 0 1rem; color: var(--aw-text-muted); font-size: 0.85rem; }`]
})
export class StockAdjustDialog {
  readonly data = inject<{ part: Part }>(MAT_DIALOG_DATA);
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<StockAdjustDialog>);

  readonly form = this.fb.group({
    type: ['Receive' as PartStockTransactionType, Validators.required],
    quantityChange: [1, Validators.required],
    notes: ['']
  });

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: AdjustPartStockRequest = {
      type: v.type!,
      quantityChange: +v.quantityChange!,
      notes: v.notes || undefined
    };
    this.ref.close(payload);
  }
}
