import { Component, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged } from 'rxjs/operators';
import { ApiService } from '../../core/services/api.service';
import { Customer, CreateCustomerRequest } from '../../core/models/api.models';
import { CustomerFormDialog } from './customer-form.dialog';

@Component({
  selector: 'app-customers',
  standalone: true,
  imports: [
    DatePipe,
    FormsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './customers.component.html',
  styleUrl: './customers.component.scss'
})
export class CustomersComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly search$ = new Subject<string>();

  readonly loading = signal(true);
  readonly customers = signal<Customer[]>([]);
  searchTerm = '';
  readonly displayedColumns = ['name', 'email', 'phone', 'vehicles', 'createdAt', 'actions'];

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(term => this.load(term));
    this.load();
  }

  private load(search?: string): void {
    this.loading.set(true);
    this.api.getCustomers(search).subscribe({
      next: result => {
        this.customers.set(result.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    this.search$.next(this.searchTerm.trim());
  }

  initials(name: string): string {
    return name.split(' ').filter(Boolean).slice(0, 2).map(p => p[0]?.toUpperCase()).join('') || '?';
  }

  add(): void {
    this.dialog
      .open(CustomerFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable' })
      .afterClosed()
      .subscribe((payload: CreateCustomerRequest | undefined) => {
        if (!payload) return;
        this.api.createCustomer(payload).subscribe({
          next: () => {
            this.snack.open('Customer added', 'Dismiss', { duration: 2500 });
            this.load(this.searchTerm.trim());
          },
          error: () => this.snack.open('Could not add customer', 'Dismiss', { duration: 3000 })
        });
      });
  }

  edit(customer: Customer): void {
    this.dialog
      .open(CustomerFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable', data: { customer } })
      .afterClosed()
      .subscribe((payload: CreateCustomerRequest | undefined) => {
        if (!payload) return;
        this.api.updateCustomer(customer.id, payload).subscribe({
          next: () => {
            this.snack.open('Customer updated', 'Dismiss', { duration: 2500 });
            this.load(this.searchTerm.trim());
          },
          error: () => this.snack.open('Could not update customer', 'Dismiss', { duration: 3000 })
        });
      });
  }

  remove(customer: Customer): void {
    if (!confirm(`Delete customer "${customer.name}"?`)) return;
    this.api.deleteCustomer(customer.id).subscribe({
      next: () => {
        this.snack.open('Customer deleted', 'Dismiss', { duration: 2500 });
        this.load(this.searchTerm.trim());
      },
      error: () => this.snack.open('Could not delete customer', 'Dismiss', { duration: 3000 })
    });
  }
}
