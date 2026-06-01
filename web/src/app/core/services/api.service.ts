import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateCustomerRequest,
  CreateVehicleRequest,
  CreateWorkOrderRequest,
  Customer,
  DashboardSummary,
  Invoice,
  InvoiceDetail,
  InvoiceStatus,
  PagedResult,
  Vehicle,
  WorkOrder,
  WorkOrderStatus
} from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiUrl;

  constructor(private readonly http: HttpClient) {}

  getDashboardSummary() {
    return this.http.get<DashboardSummary>(`${this.baseUrl}/dashboard/summary`);
  }

  getCustomers(search?: string, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Customer>>(`${this.baseUrl}/customers`, { params });
  }

  createCustomer(body: CreateCustomerRequest) {
    return this.http.post<Customer>(`${this.baseUrl}/customers`, body);
  }

  deleteCustomer(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/customers/${id}`);
  }

  getVehicles(search?: string, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (search) params = params.set('search', search);
    return this.http.get<PagedResult<Vehicle>>(`${this.baseUrl}/vehicles`, { params });
  }

  createVehicle(body: CreateVehicleRequest) {
    return this.http.post<Vehicle>(`${this.baseUrl}/vehicles`, body);
  }

  deleteVehicle(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/vehicles/${id}`);
  }

  getWorkOrders(status?: WorkOrderStatus, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<WorkOrder>>(`${this.baseUrl}/work-orders`, { params });
  }

  createWorkOrder(body: CreateWorkOrderRequest) {
    return this.http.post<WorkOrder>(`${this.baseUrl}/work-orders`, body);
  }

  updateWorkOrderStatus(id: string, status: WorkOrderStatus) {
    return this.http.patch<WorkOrder>(`${this.baseUrl}/work-orders/${id}/status`, { status });
  }

  getInvoices(status?: InvoiceStatus, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Invoice>>(`${this.baseUrl}/invoices`, { params });
  }

  getInvoice(id: string) {
    return this.http.get<InvoiceDetail>(`${this.baseUrl}/invoices/${id}`);
  }

  createInvoiceFromWorkOrder(workOrderId: string) {
    return this.http.post<Invoice>(`${this.baseUrl}/invoices/from-work-order`, { workOrderId });
  }

  updateInvoiceStatus(id: string, status: InvoiceStatus) {
    return this.http.patch<Invoice>(`${this.baseUrl}/invoices/${id}/status`, { status });
  }
}
