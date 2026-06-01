import { Component, inject, OnInit, signal } from '@angular/core';
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
import { CreateVehicleRequest, Vehicle } from '../../core/models/api.models';
import { VehicleFormDialog } from './vehicle-form.dialog';

@Component({
  selector: 'app-vehicles',
  standalone: true,
  imports: [
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
  templateUrl: './vehicles.component.html',
  styleUrl: './vehicles.component.scss'
})
export class VehiclesComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);
  private readonly search$ = new Subject<string>();

  readonly loading = signal(true);
  readonly vehicles = signal<Vehicle[]>([]);
  searchTerm = '';
  readonly displayedColumns = ['customer', 'vehicle', 'plate', 'mileage', 'color', 'actions'];

  ngOnInit(): void {
    this.search$
      .pipe(debounceTime(300), distinctUntilChanged())
      .subscribe(term => this.load(term));
    this.load();
  }

  private load(search?: string): void {
    this.loading.set(true);
    this.api.getVehicles(search).subscribe({
      next: result => {
        this.vehicles.set(result.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    this.search$.next(this.searchTerm.trim());
  }

  add(): void {
    this.dialog
      .open(VehicleFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable' })
      .afterClosed()
      .subscribe((payload: CreateVehicleRequest | undefined) => {
        if (!payload) return;
        this.api.createVehicle(payload).subscribe({
          next: () => {
            this.snack.open('Vehicle added', 'Dismiss', { duration: 2500 });
            this.load(this.searchTerm.trim());
          },
          error: () => this.snack.open('Could not add vehicle', 'Dismiss', { duration: 3000 })
        });
      });
  }

  remove(vehicle: Vehicle): void {
    if (!confirm(`Delete ${vehicle.year} ${vehicle.make} ${vehicle.model}?`)) return;
    this.api.deleteVehicle(vehicle.id).subscribe({
      next: () => {
        this.snack.open('Vehicle deleted', 'Dismiss', { duration: 2500 });
        this.load(this.searchTerm.trim());
      },
      error: () => this.snack.open('Could not delete vehicle', 'Dismiss', { duration: 3000 })
    });
  }
}
