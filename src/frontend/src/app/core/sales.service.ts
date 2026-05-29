import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse, CreateSale, PaginatedResponse, Sale } from '../models/sale.model';

@Injectable({ providedIn: 'root' })
export class SalesService {
  private readonly base = `${environment.apiBaseUrl}/sales`;

  constructor(private readonly http: HttpClient) {}

  list(page = 1, size = 10, customerName?: string): Observable<PaginatedResponse<Sale>> {
    let params = new HttpParams().set('_page', page).set('_size', size);
    if (customerName) {
      params = params.set('customerName', `${customerName}*`); // prefix match per general-api.md
    }
    return this.http.get<PaginatedResponse<Sale>>(this.base, { params });
  }

  get(id: string): Observable<Sale> {
    return this.http.get<ApiResponse<Sale>>(`${this.base}/${id}`).pipe(map((r) => r.data));
  }

  create(sale: CreateSale): Observable<Sale> {
    return this.http.post<ApiResponse<Sale>>(this.base, sale).pipe(map((r) => r.data));
  }

  cancel(id: string): Observable<Sale> {
    return this.http.patch<ApiResponse<Sale>>(`${this.base}/${id}/cancel`, {}).pipe(map((r) => r.data));
  }

  cancelItem(saleId: string, itemId: string): Observable<Sale> {
    return this.http
      .patch<ApiResponse<Sale>>(`${this.base}/${saleId}/items/${itemId}/cancel`, {})
      .pipe(map((r) => r.data));
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
