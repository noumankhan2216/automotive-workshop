import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import {
  CreateCustomerRequest,
  CreateEstimateRequest,
  CreateVehicleRequest,
  CreateWorkOrderRequest,
  Customer,
  DashboardSummary,
  Estimate,
  EstimateDetail,
  EstimateStatus,
  Invoice,
  InvoiceDetail,
  InvoiceStatus,
  PagedResult,
  ServiceCatalogItem,
  UpdateCustomerRequest,
  UpdateEstimateRequest,
  UpdateVehicleRequest,
  Vehicle,
  WorkOrder,
  WorkOrderDetail,
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

  updateCustomer(id: string, body: UpdateCustomerRequest) {
    return this.http.put<Customer>(`${this.baseUrl}/customers/${id}`, body);
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

  updateVehicle(id: string, body: UpdateVehicleRequest) {
    return this.http.put<Vehicle>(`${this.baseUrl}/vehicles/${id}`, body);
  }

  deleteVehicle(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/vehicles/${id}`);
  }

  getServiceCatalog() {
    return this.http.get<ServiceCatalogItem[]>(`${this.baseUrl}/service-catalog`);
  }

  getEstimates(status?: EstimateStatus, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<Estimate>>(`${this.baseUrl}/estimates`, { params });
  }

  getEstimate(id: string) {
    return this.http.get<EstimateDetail>(`${this.baseUrl}/estimates/${id}`);
  }

  createEstimate(body: CreateEstimateRequest) {
    return this.http.post<EstimateDetail>(`${this.baseUrl}/estimates`, body);
  }

  updateEstimate(id: string, body: UpdateEstimateRequest) {
    return this.http.put<EstimateDetail>(`${this.baseUrl}/estimates/${id}`, body);
  }

  updateEstimateStatus(id: string, status: EstimateStatus) {
    return this.http.patch<EstimateDetail>(`${this.baseUrl}/estimates/${id}/status`, { status });
  }

  convertEstimate(id: string) {
    return this.http.post<WorkOrderDetail>(`${this.baseUrl}/estimates/${id}/convert`, {});
  }

  deleteEstimate(id: string) {
    return this.http.delete<void>(`${this.baseUrl}/estimates/${id}`);
  }

  getWorkOrders(status?: WorkOrderStatus, page = 1, pageSize = 50) {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<WorkOrder>>(`${this.baseUrl}/work-orders`, { params });
  }

  getWorkOrder(id: string) {
    return this.http.get<WorkOrderDetail>(`${this.baseUrl}/work-orders/${id}`);
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

  estimatePdf(id: string) {
    return this.http.get(`${this.baseUrl}/estimates/${id}/pdf`, { responseType: 'blob' });
  }

  workOrderPdf(id: string) {
    return this.http.get(`${this.baseUrl}/work-orders/${id}/pdf`, { responseType: 'blob' });
  }

  invoicePdf(id: string) {
    return this.http.get(`${this.baseUrl}/invoices/${id}/pdf`, { responseType: 'blob' });
  }
}
