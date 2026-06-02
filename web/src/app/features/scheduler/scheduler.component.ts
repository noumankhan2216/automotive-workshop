import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe, NgStyle } from '@angular/common';
import { Router } from '@angular/router';
import { CdkDragDrop, DragDropModule } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../core/services/api.service';
import { ScheduleEvent, WorkOrder } from '../../core/models/api.models';
import { humanize, workOrderBadge } from '../../core/utils/status.util';

type CalendarView = 'day' | 'week' | 'month';

const HOUR_START = 7;
const HOUR_END = 19;
const SLOT_PX = 48;

@Component({
  selector: 'app-scheduler',
  standalone: true,
  imports: [
    DatePipe,
    NgStyle,
    DragDropModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTooltipModule
  ],
  templateUrl: './scheduler.component.html',
  styleUrl: './scheduler.component.scss'
})
export class SchedulerComponent implements OnInit {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  private readonly snack = inject(MatSnackBar);

  readonly loading = signal(true);
  readonly view = signal<CalendarView>('week');
  readonly anchor = signal(this.startOfWeek(new Date()));
  readonly events = signal<ScheduleEvent[]>([]);
  readonly unscheduled = signal<WorkOrder[]>([]);

  readonly hourSlots = Array.from({ length: HOUR_END - HOUR_START }, (_, i) => HOUR_START + i);
  readonly slotPx = SLOT_PX;

  readonly badge = workOrderBadge;
  readonly humanize = humanize;

  readonly rangeLabel = computed(() => {
    const a = this.anchor();
    const v = this.view();
    if (v === 'day') return a.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
    if (v === 'month') return a.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    const end = new Date(a);
    end.setDate(end.getDate() + 6);
    return `${a.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – ${end.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}`;
  });

  dayDropIds(): string[] {
    return this.days().map((_, i) => `day-${i}`);
  }

  allDropListIds(): string[] {
    return ['unscheduled', ...this.dayDropIds()];
  }

  readonly days = computed(() => {
    const a = this.anchor();
    const v = this.view();
    const count = v === 'day' ? 1 : v === 'week' ? 7 : this.daysInMonth(a);
    return Array.from({ length: count }, (_, i) => {
      const d = new Date(a);
      if (v === 'month') d.setDate(1 + i);
      else d.setDate(a.getDate() + i);
      return d;
    });
  });

  ngOnInit(): void {
    this.load();
  }

  setView(v: CalendarView): void {
    this.view.set(v);
    if (v === 'month') this.anchor.set(this.startOfMonth(this.anchor()));
    else if (v === 'week') this.anchor.set(this.startOfWeek(this.anchor()));
    this.load();
  }

  prev(): void {
    const a = new Date(this.anchor());
    const v = this.view();
    if (v === 'day') a.setDate(a.getDate() - 1);
    else if (v === 'week') a.setDate(a.getDate() - 7);
    else a.setMonth(a.getMonth() - 1);
    this.anchor.set(v === 'month' ? this.startOfMonth(a) : v === 'week' ? this.startOfWeek(a) : a);
    this.load();
  }

  next(): void {
    const a = new Date(this.anchor());
    const v = this.view();
    if (v === 'day') a.setDate(a.getDate() + 1);
    else if (v === 'week') a.setDate(a.getDate() + 7);
    else a.setMonth(a.getMonth() + 1);
    this.anchor.set(v === 'month' ? this.startOfMonth(a) : v === 'week' ? this.startOfWeek(a) : a);
    this.load();
  }

  today(): void {
    const now = new Date();
    const v = this.view();
    this.anchor.set(v === 'month' ? this.startOfMonth(now) : v === 'week' ? this.startOfWeek(now) : now);
    this.load();
  }

  load(): void {
    this.loading.set(true);
    const { from, to } = this.rangeBounds();
    this.api.getScheduleEvents(from, to).subscribe({
      next: events => {
        this.events.set(events);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });

    this.api.getWorkOrders(undefined, 1, 100).subscribe({
      next: result => {
        this.unscheduled.set(
          result.items.filter(w => !w.scheduledStartAt && w.status !== 'Cancelled' && w.status !== 'Paid')
        );
      }
    });
  }

  eventsForDay(day: Date): ScheduleEvent[] {
    const start = new Date(day);
    start.setHours(0, 0, 0, 0);
    const end = new Date(day);
    end.setHours(23, 59, 59, 999);
    return this.events().filter(e => {
      const s = new Date(e.scheduledStartAt);
      return s >= start && s <= end;
    });
  }

  eventStyle(ev: ScheduleEvent, day: Date): Record<string, string> {
    const start = new Date(ev.scheduledStartAt);
    const end = new Date(ev.scheduledEndAt);
    if (start.toDateString() !== day.toDateString()) return { display: 'none' };
    const top = (start.getHours() + start.getMinutes() / 60 - HOUR_START) * SLOT_PX;
    const height = Math.max(((end.getTime() - start.getTime()) / 3_600_000) * SLOT_PX, 28);
    return { top: `${top}px`, height: `${height}px` };
  }

  onCalendarDrop(day: Date, event: CdkDragDrop<ScheduleEvent[] | WorkOrder[]>): void {
    const wo = event.item.data as WorkOrder | ScheduleEvent | undefined;
    if (!wo) return;

    const workOrderId = 'workOrderId' in wo ? wo.workOrderId : wo.id;
    const { start, end } = this.scheduleFromDrop(day, event, wo);

    this.api
      .updateWorkOrderSchedule(workOrderId, {
        scheduledStartAt: start.toISOString(),
        scheduledEndAt: end.toISOString(),
        assignedToUserId: 'assignedToUserId' in wo ? wo.assignedToUserId : undefined
      })
      .subscribe({
        next: () => {
          this.snack.open('Job scheduled', 'Dismiss', { duration: 2500 });
          this.load();
        },
        error: err => this.snack.open(err?.error?.message ?? 'Could not schedule', 'Dismiss', { duration: 3500 })
      });
  }

  openJob(id: string): void {
    this.router.navigate(['/work-orders', id]);
  }

  /** Snap drop position to 30-minute increments on the day column. */
  private scheduleFromDrop(
    day: Date,
    event: CdkDragDrop<ScheduleEvent[] | WorkOrder[]>,
    wo: WorkOrder | ScheduleEvent
  ): { start: Date; end: Date } {
    const y = event.dropPoint?.y ?? SLOT_PX * 2;
    const minutesFromGridStart = (y / SLOT_PX) * 60;
    const snapped = Math.round(minutesFromGridStart / 30) * 30;
    const totalMinutes = HOUR_START * 60 + Math.max(0, Math.min(snapped, (HOUR_END - HOUR_START) * 60 - 30));

    const start = new Date(day);
    start.setHours(Math.floor(totalMinutes / 60), totalMinutes % 60, 0, 0);

    let durationMs = 2 * 3_600_000;
    if ('scheduledStartAt' in wo && wo.scheduledStartAt && wo.scheduledEndAt) {
      durationMs = new Date(wo.scheduledEndAt).getTime() - new Date(wo.scheduledStartAt).getTime();
      if (durationMs < 30 * 60_000) durationMs = 2 * 3_600_000;
    }

    const end = new Date(start.getTime() + durationMs);
    return { start, end };
  }

  private rangeBounds(): { from: string; to: string } {
    const days = this.days();
    const from = new Date(days[0]);
    from.setHours(0, 0, 0, 0);
    const to = new Date(days[days.length - 1]);
    to.setDate(to.getDate() + 1);
    to.setHours(0, 0, 0, 0);
    return { from: from.toISOString(), to: to.toISOString() };
  }

  private startOfWeek(d: Date): Date {
    const r = new Date(d);
    r.setDate(r.getDate() - r.getDay());
    r.setHours(0, 0, 0, 0);
    return r;
  }

  private startOfMonth(d: Date): Date {
    return new Date(d.getFullYear(), d.getMonth(), 1);
  }

  private daysInMonth(d: Date): number {
    return new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate();
  }
}
