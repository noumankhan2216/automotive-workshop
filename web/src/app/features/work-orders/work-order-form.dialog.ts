import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ApiService } from '../../core/services/api.service';
import { CreateWorkOrderRequest, Customer, Part, ServiceCatalogItem, Vehicle } from '../../core/models/api.models';

@Component({
  selector: 'app-work-order-form-dialog',
  standalone: true,
  imports: [
    CurrencyPipe,
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <h2 mat-dialog-title>New Work Order</h2>
    <form [formGroup]="form" (ngSubmit)="save()">
      <mat-dialog-content>
        <div class="row-2">
          <mat-form-field appearance="outline">
            <mat-label>Customer</mat-label>
            <mat-select formControlName="customerId" required>
              @for (c of customers(); track c.id) {
                <mat-option [value]="c.id">{{ c.name }}</mat-option>
              }
            </mat-select>
          </mat-form-field>

          <mat-form-field appearance="outline">
            <mat-label>Vehicle</mat-label>
            <mat-select formControlName="vehicleId" required [disabled]="!selectedCustomerId()">
              @for (v of vehiclesForCustomer(); track v.id) {
                <mat-option [value]="v.id">{{ v.year }} {{ v.make }} {{ v.model }}</mat-option>
              }
            </mat-select>
            @if (selectedCustomerId() && vehiclesForCustomer().length === 0) {
              <mat-hint>No vehicles for this customer</mat-hint>
            }
          </mat-form-field>
        </div>

        <div class="line-items">
          <div class="line-items__head">
            <span>Line Items</span>
            <button mat-stroked-button type="button" (click)="addItem()">
              <mat-icon>add</mat-icon> Add item
            </button>
          </div>

          @for (item of items.controls; track $index) {
            <div class="line-item" [formGroup]="$any(item)">
              <mat-form-field appearance="outline" class="li-cat">
                <mat-label>Service</mat-label>
                <mat-select formControlName="serviceCatalogItemId"
                            (selectionChange)="applyCatalog($index, $event.value)">
                  <mat-option [value]="null">Custom…</mat-option>
                  @for (s of catalog(); track s.id) {
                    <mat-option [value]="s.id">{{ s.name }}</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="li-cat">
                <mat-label>Part (inventory)</mat-label>
                <mat-select formControlName="partId" (selectionChange)="applyPart($index, $event.value)">
                  <mat-option [value]="null">None</mat-option>
                  @for (p of parts(); track p.id) {
                    <mat-option [value]="p.id">{{ p.sku }} — {{ p.name }} ({{ p.quantityOnHand }})</mat-option>
                  }
                </mat-select>
              </mat-form-field>
              <mat-form-field appearance="outline" class="li-desc">
                <mat-label>Description</mat-label>
                <input matInput formControlName="description" required />
              </mat-form-field>
              <mat-form-field appearance="outline" class="li-num">
                <mat-label>Qty</mat-label>
                <input matInput type="number" formControlName="quantity" required />
              </mat-form-field>
              <mat-form-field appearance="outline" class="li-num">
                <mat-label>Unit Price</mat-label>
                <input matInput type="number" formControlName="unitPrice" required />
              </mat-form-field>
              <button mat-icon-button type="button" class="li-remove" (click)="removeItem($index)"
                      [disabled]="items.length === 1" aria-label="Remove item">
                <mat-icon>close</mat-icon>
              </button>
            </div>
          }
        </div>

        <mat-form-field appearance="outline" class="full">
          <mat-label>Customer notes</mat-label>
          <textarea matInput rows="2" formControlName="customerNotes"></textarea>
        </mat-form-field>

        <div class="total-row">
          <span>Estimated total</span>
          <strong>{{ total() | currency }}</strong>
        </div>
      </mat-dialog-content>

      <mat-dialog-actions align="end">
        <button mat-button type="button" mat-dialog-close>Cancel</button>
        <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid">
          Create Work Order
        </button>
      </mat-dialog-actions>
    </form>
  `,
  styles: [`
    mat-dialog-content { min-width: 560px; padding-top: 0.5rem; }
    .row-2 { display: grid; grid-template-columns: 1fr 1fr; gap: 1rem; }
    .row-2 mat-form-field, .full { width: 100%; }
    .full { display: block; }
    .line-items { margin: 0.5rem 0 1rem; }
    .line-items__head {
      display: flex; align-items: center; justify-content: space-between;
      font-weight: 600; font-size: 0.9rem; margin-bottom: 0.5rem;
    }
    .line-item { display: grid; grid-template-columns: 130px 130px 1fr 72px 100px 40px; gap: 0.6rem; align-items: center; }
    .li-cat, .li-desc, .li-num { width: 100%; }
    .li-remove { margin-bottom: 1.2rem; }
    .total-row {
      display: flex; justify-content: space-between; align-items: center;
      padding: 0.75rem 1rem; background: #f8fafc; border-radius: 12px; font-size: 0.95rem;
    }
    .total-row strong { font-size: 1.2rem; }
    @media (max-width: 620px) {
      mat-dialog-content { min-width: unset; }
      .row-2 { grid-template-columns: 1fr; }
      .line-item { grid-template-columns: 1fr 1fr; }
    }
  `]
})
export class WorkOrderFormDialog implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly api = inject(ApiService);
  private readonly dialogRef = inject(MatDialogRef<WorkOrderFormDialog>);

  readonly customers = signal<Customer[]>([]);
  readonly vehicles = signal<Vehicle[]>([]);
  readonly catalog = signal<ServiceCatalogItem[]>([]);
  readonly parts = signal<Part[]>([]);

  readonly form = this.fb.nonNullable.group({
    customerId: ['', Validators.required],
    vehicleId: ['', Validators.required],
    customerNotes: [''],
    items: this.fb.array([this.createItem()])
  });

  private readonly customerId = toSignal(this.form.controls.customerId.valueChanges, {
    initialValue: this.form.controls.customerId.value
  });
  readonly selectedCustomerId = computed(() => this.customerId());
  readonly vehiclesForCustomer = computed(() =>
    this.vehicles().filter(v => v.customerId === this.selectedCustomerId())
  );

  private readonly itemsChange = toSignal(this.form.controls.items.valueChanges, {
    initialValue: this.form.controls.items.value
  });
  readonly total = computed(() =>
    (this.itemsChange() ?? []).reduce(
      (sum, i) => sum + (Number(i.quantity) || 0) * (Number(i.unitPrice) || 0),
      0
    )
  );

  get items(): FormArray {
    return this.form.controls.items;
  }

  private createItem() {
    return this.fb.nonNullable.group({
      serviceCatalogItemId: <string | null>null,
      partId: <string | null>null,
      description: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]]
    });
  }

  addItem(): void {
    this.items.push(this.createItem());
  }

  removeItem(index: number): void {
    if (this.items.length > 1) this.items.removeAt(index);
  }

  applyCatalog(index: number, catalogId: string | null): void {
    if (!catalogId) return;
    const item = this.catalog().find(c => c.id === catalogId);
    if (!item) return;
    this.items.at(index).patchValue({ description: item.name, unitPrice: item.defaultPrice });
  }

  applyPart(index: number, partId: string | null): void {
    if (!partId) return;
    const part = this.parts().find(p => p.id === partId);
    if (!part) return;
    this.items.at(index).patchValue({
      description: part.name,
      unitPrice: part.unitPrice,
      quantity: 1
    });
  }

  ngOnInit(): void {
    this.api.getCustomers(undefined, 1, 200).subscribe(res => this.customers.set(res.items));
    this.api.getVehicles(undefined, 1, 500).subscribe(res => this.vehicles.set(res.items));
    this.api.getServiceCatalog().subscribe(items => this.catalog.set(items));
    this.api.getParts(undefined, false, 1, 200).subscribe(res => this.parts.set(res.items.filter(p => p.isActive)));

    this.form.controls.customerId.valueChanges.subscribe(() =>
      this.form.controls.vehicleId.setValue('')
    );
  }

  save(): void {
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    const payload: CreateWorkOrderRequest = {
      customerId: v.customerId,
      vehicleId: v.vehicleId,
      customerNotes: v.customerNotes?.trim() || undefined,
      items: v.items.map(i => ({
        serviceCatalogItemId: i.serviceCatalogItemId ?? undefined,
        partId: i.partId ?? undefined,
        description: i.description.trim(),
        quantity: Number(i.quantity),
        unitPrice: Number(i.unitPrice)
      }))
    };
    this.dialogRef.close(payload);
  }
}
