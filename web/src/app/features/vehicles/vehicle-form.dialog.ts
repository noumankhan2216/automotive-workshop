import { Component, inject, OnInit, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { ApiService } from '../../core/services/api.service';
import { CreateVehicleRequest, Customer, Vehicle } from '../../core/models/api.models';

export interface VehicleFormData {
  vehicle?: Vehicle;
}

@Component({
  selector: 'app-vehicle-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule
  ],
  template: `
    <h2 mat-dialog-title>{{ isEdit ? 'Edit Vehicle' : 'New Vehicle' }}</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="form-grid">
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Customer</mat-label>
          <mat-select formControlName="customerId" required>
            @for (c of customers(); track c.id) {
              <mat-option [value]="c.id">{{ c.name }}</mat-option>
            }
          </mat-select>
          @if (form.controls.customerId.hasError('required') && form.controls.customerId.touched) {
            <mat-error>Select a customer</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Make</mat-label>
          <input matInput formControlName="make" required />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Model</mat-label>
          <input matInput formControlName="model" required />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Year</mat-label>
          <input matInput type="number" formControlName="year" required />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>License Plate</mat-label>
          <input matInput formControlName="licensePlate" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Mileage</mat-label>
          <input matInput type="number" formControlName="mileage" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Color</mat-label>
          <input matInput formControlName="color" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="span-2">
          <mat-label>VIN</mat-label>
          <input matInput formControlName="vin" />
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          {{ isEdit ? 'Save Changes' : 'Save Vehicle' }}
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`
    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.25rem 1rem;
      min-width: 480px;
      padding-top: 0.5rem;
    }
    .span-2 { grid-column: 1 / -1; }
    mat-form-field { width: 100%; }
    @media (max-width: 560px) {
      .form-grid { grid-template-columns: 1fr; min-width: unset; }
    }
  `]
})
export class VehicleFormDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<VehicleFormDialog>);
  private readonly data = inject<VehicleFormData>(MAT_DIALOG_DATA, { optional: true });

  readonly isEdit = !!this.data?.vehicle;
  readonly customers = signal<Customer[]>([]);

  readonly form = this.fb.nonNullable.group({
    customerId: [this.data?.vehicle?.customerId ?? '', Validators.required],
    make: [this.data?.vehicle?.make ?? '', Validators.required],
    model: [this.data?.vehicle?.model ?? '', Validators.required],
    year: [this.data?.vehicle?.year ?? new Date().getFullYear(), [Validators.required, Validators.min(1900)]],
    licensePlate: [this.data?.vehicle?.licensePlate ?? ''],
    mileage: [this.data?.vehicle?.mileage ?? (null as number | null)],
    color: [this.data?.vehicle?.color ?? ''],
    vin: [this.data?.vehicle?.vin ?? '']
  });

  ngOnInit(): void {
    this.api.getCustomers(undefined, 1, 200).subscribe(res => this.customers.set(res.items));
    if (this.isEdit) this.form.controls.customerId.disable();
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateVehicleRequest = {
      customerId: v.customerId,
      make: v.make.trim(),
      model: v.model.trim(),
      year: Number(v.year),
      licensePlate: v.licensePlate?.trim() || undefined,
      mileage: v.mileage != null ? Number(v.mileage) : undefined,
      color: v.color?.trim() || undefined,
      vin: v.vin?.trim() || undefined
    };
    this.dialogRef.close(payload);
  }
}
