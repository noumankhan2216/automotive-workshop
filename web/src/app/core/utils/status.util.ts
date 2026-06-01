import { InvoiceStatus, WorkOrderStatus } from '../models/api.models';

const WORK_ORDER_BADGE: Record<WorkOrderStatus, string> = {
  Draft: 'badge--gray',
  InProgress: 'badge--blue',
  WaitingParts: 'badge--amber',
  Completed: 'badge--green',
  Invoiced: 'badge--purple',
  Paid: 'badge--teal',
  Cancelled: 'badge--red'
};

const INVOICE_BADGE: Record<InvoiceStatus, string> = {
  Draft: 'badge--gray',
  Sent: 'badge--blue',
  Paid: 'badge--green',
  Overdue: 'badge--red',
  Void: 'badge--gray'
};

// Order MUST match the C# enums so numeric values map to the right name
// (in case the API serializes enums as integers).
export const WORK_ORDER_STATUSES = Object.keys(WORK_ORDER_BADGE) as WorkOrderStatus[];
export const INVOICE_STATUSES = Object.keys(INVOICE_BADGE) as InvoiceStatus[];

/** Accepts a string name or a numeric enum value and returns the canonical name. */
export function normalizeWorkOrderStatus(value: WorkOrderStatus | number | string): WorkOrderStatus {
  if (typeof value === 'number') return WORK_ORDER_STATUSES[value] ?? 'Draft';
  return (value as WorkOrderStatus) ?? 'Draft';
}

export function normalizeInvoiceStatus(value: InvoiceStatus | number | string): InvoiceStatus {
  if (typeof value === 'number') return INVOICE_STATUSES[value] ?? 'Draft';
  return (value as InvoiceStatus) ?? 'Draft';
}

export function workOrderBadge(status: WorkOrderStatus | number | string): string {
  return WORK_ORDER_BADGE[normalizeWorkOrderStatus(status)] ?? 'badge--gray';
}

export function invoiceBadge(status: InvoiceStatus | number | string): string {
  return INVOICE_BADGE[normalizeInvoiceStatus(status)] ?? 'badge--gray';
}

export function humanize(value: unknown): string {
  if (value == null) return '';
  return String(value).replace(/([a-z])([A-Z])/g, '$1 $2');
}
