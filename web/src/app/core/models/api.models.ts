export interface LoginRequest {
  email: string;
  password: string;
}

export interface User {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface Customer {
  id: string;
  name: string;
  email?: string;
  phone?: string;
  address?: string;
  notes?: string;
  vehicleCount: number;
  createdAt: string;
}

export interface Vehicle {
  id: string;
  customerId: string;
  customerName: string;
  make: string;
  model: string;
  year: number;
  vin?: string;
  licensePlate?: string;
  mileage?: number;
  color?: string;
}

export interface WorkOrder {
  id: string;
  workOrderNumber: string;
  customerId: string;
  customerName: string;
  vehicleId: string;
  vehicleDescription: string;
  status: WorkOrderStatus;
  assignedToUserId?: string;
  openedAt: string;
  completedAt?: string;
  totalAmount: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  workOrderId?: string;
  status: InvoiceStatus;
  subTotal: number;
  taxAmount: number;
  total: number;
  issuedAt: string;
  dueDate?: string;
  paidAt?: string;
}

export interface InvoiceLine {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface InvoiceDetail {
  id: string;
  invoiceNumber: string;
  customerId: string;
  customerName: string;
  workOrderId?: string;
  status: InvoiceStatus;
  subTotal: number;
  taxRate: number;
  taxAmount: number;
  total: number;
  issuedAt: string;
  dueDate?: string;
  paidAt?: string;
  notes?: string;
  lines: InvoiceLine[];
}

export interface DashboardSummary {
  revenueToday: number;
  revenueThisWeek: number;
  revenueThisMonth: number;
  openWorkOrders: number;
  completedWorkOrdersThisMonth: number;
  outstandingInvoices: number;
  outstandingAmount: number;
}

export type WorkOrderStatus =
  | 'Draft'
  | 'InProgress'
  | 'WaitingParts'
  | 'Completed'
  | 'Invoiced'
  | 'Paid'
  | 'Cancelled';

export type InvoiceStatus = 'Draft' | 'Sent' | 'Paid' | 'Overdue' | 'Void';

export interface CreateCustomerRequest {
  name: string;
  email?: string;
  phone?: string;
  address?: string;
  notes?: string;
}

export interface CreateVehicleRequest {
  customerId: string;
  make: string;
  model: string;
  year: number;
  vin?: string;
  licensePlate?: string;
  mileage?: number;
  color?: string;
}

export interface WorkOrderItemInput {
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateWorkOrderRequest {
  customerId: string;
  vehicleId: string;
  customerNotes?: string;
  internalNotes?: string;
  items: WorkOrderItemInput[];
}
