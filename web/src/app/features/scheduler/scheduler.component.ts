import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe, NgStyle } from '@angular/common';
import { Router } from '@angular/router';
import { CdkDragDrop, CdkDragStart, DragDropModule } from '@angular/cdk/drag-drop';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ApiService } from '../../core/services/api.service';
import { ScheduleEvent, WorkOrder } from '../../core/models/api.models';
import { humanize, workOrderBadge } from '../../core/utils/status.util';
import {
  formatLocalTimeRange,
  localDateTimeFromTotalMinutes,
  toApiScheduleTime,
  localEndOfDay,
  localStartOfDay,
  minutesSinceLocalMidnight,
  overlapsLocalDay,
  parseScheduleInstant
} from './scheduler-time.util';

type CalendarView = 'day' | 'week' | 'month';

const HOUR_START = 7;
const HOUR_END = 19;
const SLOT_PX = 48;
const SNAP_MINUTES = 30;
const HALF_HOUR_PX = SLOT_PX / 2;
const DEFAULT_DURATION_MS = 2 * 3_600_000;

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
  readonly saving = signal(false);
  readonly view = signal<CalendarView>('week');
  readonly anchor = signal(this.startOfWeek(new Date()));
  readonly events = signal<ScheduleEvent[]>([]);
  readonly unscheduled = signal<WorkOrder[]>([]);

  private readonly dropListDataById = new Map<string, unknown[]>();

  readonly connectedDropIds = computed(() => [
    'unscheduled',
    ...this.days().map((_, i) => `day-${i}`)
  ]);

  readonly hourSlots = Array.from({ length: HOUR_END - HOUR_START }, (_, i) => HOUR_START + i);
  readonly slotPx = SLOT_PX;

  readonly badge = workOrderBadge;
  readonly humanize = humanize;
  readonly formatTimeRange = formatLocalTimeRange;

  private suppressClickUntil = 0;

  readonly rangeLabel = computed(() => {
    const a = this.anchor();
    const v = this.view();
    if (v === 'day') return a.toLocaleDateString(undefined, { weekday: 'long', month: 'long', day: 'numeric', year: 'numeric' });
    if (v === 'month') return a.toLocaleDateString(undefined, { month: 'long', year: 'numeric' });
    const end = new Date(a);
    end.setDate(end.getDate() + 6);
    return `${a.toLocaleDateString(undefined, { month: 'short', day: 'numeric' })} – ${end.toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })}`;
  });

  dropListData(id: string): unknown[] {
    let data = this.dropListDataById.get(id);
    if (!data) {
      data = [];
      this.dropListDataById.set(id, data);
    }
    return data;
  }

  readonly days = computed(() => {
    const a = this.anchor();
    const v = this.view();
    const count = v === 'day' ? 1 : v === 'week' ? 7 : this.daysInMonth(a);
    return Array.from({ length: count }, (_, i) => {
      const y = a.getFullYear();
      const m = a.getMonth();
      if (v === 'month') return new Date(y, m, 1 + i);
      const d = new Date(y, m, a.getDate() + i);
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

  load(opts?: { silent?: boolean }): void {
    const scrollEl = document.querySelector('.sched-calendar');
    const scrollTop = scrollEl?.scrollTop ?? 0;

    if (!opts?.silent) this.loading.set(true);
    const { from, to } = this.rangeBounds();
    this.api.getScheduleEvents(from, to).subscribe({
      next: events => {
        this.events.set(events);
        if (!opts?.silent) this.loading.set(false);
        this.restoreCalendarScroll(scrollTop);
      },
      error: () => {
        if (!opts?.silent) this.loading.set(false);
        this.restoreCalendarScroll(scrollTop);
      }
    });

    this.api.getWorkOrders(undefined, 1, 100).subscribe({
      next: result => {
        this.unscheduled.set(
          result.items.filter(w => !w.scheduledStartAt && w.status !== 'Cancelled' && w.status !== 'Paid')
        );
      }
    });
  }

  private restoreCalendarScroll(scrollTop: number): void {
    requestAnimationFrame(() => {
      const el = document.querySelector('.sched-calendar');
      if (el) el.scrollTop = scrollTop;
    });
  }

  eventsForDay(day: Date): ScheduleEvent[] {
    return this.events().filter(e => {
      const start = parseScheduleInstant(e.scheduledStartAt);
      const end = parseScheduleInstant(e.scheduledEndAt);
      return overlapsLocalDay(start, end, day);
    });
  }

  eventStyle(ev: ScheduleEvent, day: Date): Record<string, string> {
    const start = parseScheduleInstant(ev.scheduledStartAt);
    const end = parseScheduleInstant(ev.scheduledEndAt);
    if (!overlapsLocalDay(start, end, day)) return { display: 'none' };

    const dayStart = localStartOfDay(day);
    const dayEnd = localEndOfDay(day);
    const segmentStart = start < dayStart ? dayStart : start;
    const segmentEnd = end > dayEnd ? dayEnd : end;

    const startMins = minutesSinceLocalMidnight(segmentStart);
    const endMins = minutesSinceLocalMidnight(segmentEnd);
    const gridStartMins = HOUR_START * 60;
    const gridEndMins = HOUR_END * 60;

    const visibleStart = Math.max(startMins, gridStartMins);
    const visibleEnd = Math.min(endMins, gridEndMins);
    if (visibleEnd <= visibleStart) return { display: 'none' };

    const top = ((visibleStart - gridStartMins) / 60) * SLOT_PX;
    const height = Math.max(((visibleEnd - visibleStart) / 60) * SLOT_PX, 36);
    return { top: `${top}px`, height: `${height}px` };
  }

  onDragStarted(_ev: CdkDragStart): void {
    this.suppressClickUntil = Date.now() + 400;
  }

  onEventClick(workOrderId: string, ev: MouseEvent): void {
    if (Date.now() < this.suppressClickUntil) {
      ev.preventDefault();
      ev.stopPropagation();
      return;
    }
    this.openJob(workOrderId);
  }

  onCalendarDrop(day: Date, dropEvent: CdkDragDrop<unknown[]>): void {
    if (this.saving()) return;

    const wo = dropEvent.item.data as WorkOrder | ScheduleEvent | undefined;
    if (!wo) return;

    const workOrderId = 'workOrderId' in wo ? wo.workOrderId : wo.id;
    const container = dropEvent.container.element.nativeElement as HTMLElement;
    const y = this.eventTopYInColumn(container, dropEvent);
    const { start, end } = this.scheduleFromPointerY(day, y, wo);

    const optimistic = this.buildScheduleEvent(wo, workOrderId, start, end);
    this.upsertEvent(optimistic);
    if (!('workOrderId' in wo)) {
      this.unscheduled.update(list => list.filter(w => w.id !== workOrderId));
    }

    this.saving.set(true);
    this.api
      .updateWorkOrderSchedule(workOrderId, {
        scheduledStartAt: toApiScheduleTime(start),
        scheduledEndAt: toApiScheduleTime(end),
        assignedToUserId: 'assignedToUserId' in wo ? wo.assignedToUserId ?? undefined : undefined
      })
      .subscribe({
        next: saved => {
          this.saving.set(false);
          this.upsertEvent(saved);
          this.snack.open(
            `Scheduled ${formatLocalTimeRange(saved.scheduledStartAt, saved.scheduledEndAt)}`,
            'Dismiss',
            { duration: 3000 }
          );
        },
        error: err => {
          this.saving.set(false);
          this.load({ silent: true });
          this.snack.open(err?.error?.message ?? 'Could not schedule', 'Dismiss', { duration: 3500 });
        }
      });
  }

  private upsertEvent(ev: ScheduleEvent): void {
    const rest = this.events().filter(e => e.workOrderId !== ev.workOrderId);
    this.events.set([...rest, ev]);
  }

  private buildScheduleEvent(
    wo: WorkOrder | ScheduleEvent,
    workOrderId: string,
    start: Date,
    end: Date
  ): ScheduleEvent {
    const startIso = toApiScheduleTime(start);
    const endIso = toApiScheduleTime(end);
    if ('workOrderId' in wo) {
      return { ...wo, scheduledStartAt: startIso, scheduledEndAt: endIso };
    }
    return {
      workOrderId,
      workOrderNumber: wo.workOrderNumber,
      customerName: wo.customerName,
      vehicleDescription: wo.vehicleDescription,
      status: wo.status,
      assignedToUserId: wo.assignedToUserId,
      assignedToUserName: wo.assignedToUserName,
      bayLabel: wo.bayLabel,
      scheduledStartAt: startIso,
      scheduledEndAt: endIso,
      totalAmount: wo.totalAmount
    };
  }

  openJob(id: string): void {
    this.router.navigate(['/work-orders', id]);
  }

  /**
   * Y of the event top inside the day column body (0 = 7:00 row).
   * Uses where CDK places the placeholder/preview — not pointer minus drag-start grab offset
   * (that offset caused a fixed ~2h early shift for 2-hour events).
   */
  private eventTopYInColumn(columnEl: HTMLElement, dropEvent: CdkDragDrop<unknown[]>): number {
    const maxY = (HOUR_END - HOUR_START) * SLOT_PX - 1;
    const columnRect = columnEl.getBoundingClientRect();
    let rawY: number | null = null;

    const placeholder = columnEl.querySelector('.cdk-drag-placeholder') as HTMLElement | null;
    if (placeholder) {
      rawY = placeholder.getBoundingClientRect().top - columnRect.top;
    }

    if (rawY == null) {
      const preview = document.querySelector('.cdk-drag-preview') as HTMLElement | null;
      if (preview) {
        rawY = preview.getBoundingClientRect().top - columnRect.top;
      }
    }

    if (rawY == null && dropEvent.dropPoint?.y != null) {
      rawY = dropEvent.dropPoint.y;
    }

    if (rawY == null) {
      rawY = this.yFromHourSlotUnderPointer(columnEl, dropEvent);
    }

    return this.snapYToGrid(Math.max(0, Math.min(rawY ?? HALF_HOUR_PX * 2, maxY)));
  }

  private snapYToGrid(rawY: number): number {
    const slotIndex = Math.round(rawY / HALF_HOUR_PX);
    const maxSlot = ((HOUR_END - HOUR_START) * 60) / SNAP_MINUTES - 1;
    return Math.max(0, Math.min(slotIndex, maxSlot)) * HALF_HOUR_PX;
  }

  private clientYFromDrop(dropEvent: CdkDragDrop<unknown[]>): number | null {
    const e = dropEvent.event;
    if (e instanceof MouseEvent) return e.clientY;
    if (e instanceof TouchEvent && e.changedTouches.length) return e.changedTouches[0].clientY;
    return null;
  }

  /** Fallback: map pointer to hour-slot layout inside the column. */
  private yFromHourSlotUnderPointer(columnEl: HTMLElement, dropEvent: CdkDragDrop<unknown[]>): number | null {
    const clientY = this.clientYFromDrop(dropEvent);
    if (clientY == null) return null;

    const slots = columnEl.querySelectorAll<HTMLElement>('.hour-slot');
    if (!slots.length) return null;

    const columnRect = columnEl.getBoundingClientRect();
    for (let i = slots.length - 1; i >= 0; i--) {
      const slotRect = slots[i].getBoundingClientRect();
      if (clientY >= slotRect.top - 1) {
        const offsetInHour = Math.max(0, Math.min(clientY - slotRect.top, SLOT_PX));
        return i * SLOT_PX + offsetInHour;
      }
    }
    return clientY - columnRect.top;
  }

  private scheduleFromPointerY(
    day: Date,
    snappedY: number,
    wo: WorkOrder | ScheduleEvent
  ): { start: Date; end: Date } {
    const slotIndex = snappedY / HALF_HOUR_PX;
    const totalMinutes = HOUR_START * 60 + slotIndex * SNAP_MINUTES;

    const start = localDateTimeFromTotalMinutes(day, totalMinutes);

    let durationMs = DEFAULT_DURATION_MS;
    if ('scheduledStartAt' in wo && wo.scheduledStartAt && wo.scheduledEndAt) {
      const prev =
        parseScheduleInstant(wo.scheduledEndAt).getTime() -
        parseScheduleInstant(wo.scheduledStartAt).getTime();
      if (prev >= SNAP_MINUTES * 60_000) durationMs = prev;
    }

    let end = new Date(start.getTime() + durationMs);
    const gridEnd = localDateTimeFromTotalMinutes(day, HOUR_END * 60);
    if (end > gridEnd) end = gridEnd;

    return { start, end };
  }

  private rangeBounds(): { from: string; to: string } {
    const days = this.days();
    const from = localStartOfDay(days[0]);
    const to = localStartOfDay(days[days.length - 1]);
    to.setDate(to.getDate() + 1);
    return { from: from.toISOString(), to: to.toISOString() };
  }

  private startOfWeek(d: Date): Date {
    const r = new Date(d.getFullYear(), d.getMonth(), d.getDate());
    r.setDate(r.getDate() - r.getDay());
    return r;
  }

  private startOfMonth(d: Date): Date {
    return new Date(d.getFullYear(), d.getMonth(), 1);
  }

  private daysInMonth(d: Date): number {
    return new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate();
  }
}
