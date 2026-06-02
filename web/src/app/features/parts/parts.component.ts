import { Component, inject, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatTableModule } from '@angular/material/table';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ApiService } from '../../core/services/api.service';
import { Part } from '../../core/models/api.models';
import { PartFormDialog } from './part-form.dialog';
import { StockAdjustDialog } from './stock-adjust.dialog';

@Component({
  selector: 'app-parts',
  standalone: true,
  imports: [
    CurrencyPipe,
    FormsModule,
    MatTableModule,
    MatProgressSpinnerModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatTooltipModule,
    MatDialogModule
  ],
  templateUrl: './parts.component.html',
  styleUrl: './parts.component.scss'
})
export class PartsComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly parts = signal<Part[]>([]);
  searchTerm = '';
  lowStockOnly = false;
  readonly displayedColumns = ['sku', 'name', 'category', 'onHand', 'reorder', 'price', 'actions'];

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getParts(this.searchTerm.trim() || undefined, this.lowStockOnly).subscribe({
      next: result => {
        this.parts.set(result.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  add(): void {
    this.dialog
      .open(PartFormDialog, { panelClass: 'aw-dialog', autoFocus: 'first-tabbable' })
      .afterClosed()
      .subscribe(payload => {
        if (!payload) return;
        this.api.createPart(payload).subscribe({
          next: () => {
            this.snack.open('Part added', 'Dismiss', { duration: 2500 });
            this.load();
          },
          error: () => this.snack.open('Could not add part', 'Dismiss', { duration: 3000 })
        });
      });
  }

  edit(part: Part): void {
    this.dialog
      .open(PartFormDialog, { panelClass: 'aw-dialog', data: { part } })
      .afterClosed()
      .subscribe(payload => {
        if (!payload) return;
        this.api.updatePart(part.id, payload).subscribe({
          next: () => {
            this.snack.open('Part updated', 'Dismiss', { duration: 2500 });
            this.load();
          },
          error: () => this.snack.open('Could not update part', 'Dismiss', { duration: 3000 })
        });
      });
  }

  adjustStock(part: Part): void {
    this.dialog
      .open(StockAdjustDialog, { panelClass: 'aw-dialog', data: { part } })
      .afterClosed()
      .subscribe(payload => {
        if (!payload) return;
        this.api.adjustPartStock(part.id, payload).subscribe({
          next: () => {
            this.snack.open('Stock updated', 'Dismiss', { duration: 2500 });
            this.load();
          },
          error: err =>
            this.snack.open(err?.error?.message ?? 'Could not adjust stock', 'Dismiss', { duration: 3500 })
        });
      });
  }
}
