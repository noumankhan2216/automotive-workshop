import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { CreateCustomerRequest } from '../../core/models/api.models';

@Component({
  selector: 'app-customer-form-dialog',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>New Customer</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content class="form-grid">
        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Name</mat-label>
          <input matInput formControlName="name" required />
          @if (form.controls.name.hasError('required') && form.controls.name.touched) {
            <mat-error>Name is required</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Email</mat-label>
          <input matInput type="email" formControlName="email" />
          @if (form.controls.email.hasError('email')) {
            <mat-error>Enter a valid email</mat-error>
          }
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Phone</mat-label>
          <input matInput formControlName="phone" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Address</mat-label>
          <input matInput formControlName="address" />
        </mat-form-field>

        <mat-form-field appearance="outline" class="span-2">
          <mat-label>Notes</mat-label>
          <textarea matInput rows="3" formControlName="notes"></textarea>
        </mat-form-field>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          Save Customer
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`
    .form-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 0.25rem 1rem;
      min-width: 460px;
      padding-top: 0.5rem;
    }
    .span-2 { grid-column: 1 / -1; }
    mat-form-field { width: 100%; }
    @media (max-width: 540px) {
      .form-grid { grid-template-columns: 1fr; min-width: unset; }
    }
  `]
})
export class CustomerFormDialog {
  private readonly fb = inject(FormBuilder);
  private readonly dialogRef = inject(MatDialogRef<CustomerFormDialog>);

  readonly form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    email: ['', Validators.email],
    phone: [''],
    address: [''],
    notes: ['']
  });

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateCustomerRequest = {
      name: v.name.trim(),
      email: v.email?.trim() || undefined,
      phone: v.phone?.trim() || undefined,
      address: v.address?.trim() || undefined,
      notes: v.notes?.trim() || undefined
    };
    this.dialogRef.close(payload);
  }
}
