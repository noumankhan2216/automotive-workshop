import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { CreatePartRequest, Part, UpdatePartRequest } from '../../core/models/api.models';

export interface PartFormData {
  part?: Part;
}

@Component({
  selector: 'app-part-form-dialog',
  standalone: true,
  imports: [ReactiveFormsModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatCheckboxModule],
  template: `
    <h2 mat-dialog-title>{{ data.part ? 'Edit Part' : 'Add Part' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <div class="row-2">
          <mat-form-field appearance="outline"><mat-label>SKU</mat-label><input matInput formControlName="sku" required /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Name</mat-label><input matInput formControlName="name" required /></mat-form-field>
        </div>
        <mat-form-field appearance="outline" class="full"><mat-label>Category</mat-label><input matInput formControlName="category" /></mat-form-field>
        <mat-form-field appearance="outline" class="full"><mat-label>Description</mat-label><textarea matInput formControlName="description" rows="2"></textarea></mat-form-field>
        <div class="row-2">
          <mat-form-field appearance="outline"><mat-label>Unit Cost</mat-label><input matInput type="number" formControlName="unitCost" required /></mat-form-field>
          <mat-form-field appearance="outline"><mat-label>Retail Price</mat-label><input matInput type="number" formControlName="unitPrice" required /></mat-form-field>
        </div>
        <div class="row-2">
          @if (!data.part) {
            <mat-form-field appearance="outline"><mat-label>Initial Qty</mat-label><input matInput type="number" formControlName="quantityOnHand" required /></mat-form-field>
          }
          <mat-form-field appearance="outline"><mat-label>Reorder Level</mat-label><input matInput type="number" formControlName="reorderLevel" required /></mat-form-field>
        </div>
        @if (data.part) {
          <mat-checkbox formControlName="isActive">Active</mat-checkbox>
        }
      </mat-dialog-content>
      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">Save</button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`.full { width: 100%; } .row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }`]
})
export class PartFormDialog {
  readonly data = inject<PartFormData>(MAT_DIALOG_DATA, { optional: true }) ?? {};
  private readonly fb = inject(FormBuilder);
  private readonly ref = inject(MatDialogRef<PartFormDialog>);

  readonly form = this.fb.group({
    sku: [this.data.part?.sku ?? '', Validators.required],
    name: [this.data.part?.name ?? '', Validators.required],
    description: [this.data.part?.description ?? ''],
    category: [this.data.part?.category ?? ''],
    unitCost: [this.data.part?.unitCost ?? 0, [Validators.required, Validators.min(0)]],
    unitPrice: [this.data.part?.unitPrice ?? 0, [Validators.required, Validators.min(0)]],
    quantityOnHand: [this.data.part?.quantityOnHand ?? 0, [Validators.min(0)]],
    reorderLevel: [this.data.part?.reorderLevel ?? 0, [Validators.required, Validators.min(0)]],
    isActive: [this.data.part?.isActive ?? true]
  });

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    if (this.data.part) {
      const payload: UpdatePartRequest = {
        sku: v.sku!,
        name: v.name!,
        description: v.description || undefined,
        category: v.category || undefined,
        unitCost: +v.unitCost!,
        unitPrice: +v.unitPrice!,
        reorderLevel: +v.reorderLevel!,
        isActive: !!v.isActive
      };
      this.ref.close(payload);
    } else {
      const payload: CreatePartRequest = {
        sku: v.sku!,
        name: v.name!,
        description: v.description || undefined,
        category: v.category || undefined,
        unitCost: +v.unitCost!,
        unitPrice: +v.unitPrice!,
        quantityOnHand: +v.quantityOnHand!,
        reorderLevel: +v.reorderLevel!
      };
      this.ref.close(payload);
    }
  }
}
