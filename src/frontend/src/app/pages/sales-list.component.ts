import { CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SalesService } from '../core/sales.service';
import { Sale } from '../models/sale.model';

@Component({
  selector: 'app-sales-list',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe, DatePipe],
  template: `
    <div class="page-head">
      <div>
        <h2>Sales</h2>
        <p>Browse, filter and cancel sales records.</p>
      </div>
      <a routerLink="/sales/new"><button>+ New sale</button></a>
    </div>

    <div class="card" style="margin-bottom: 16px;">
      <div class="field-row" style="align-items: flex-end;">
        <div>
          <label for="filter">Customer name</label>
          <input id="filter" placeholder="e.g. John" [(ngModel)]="customerFilter" name="filter"
                 (keyup.enter)="load()" />
        </div>
        <button class="secondary" (click)="load()">Search</button>
        @if (customerFilter) {
          <button class="ghost" (click)="customerFilter=''; load()">Clear</button>
        }
      </div>
    </div>

    @if (error) { <div class="alert error">{{ error }}</div> }

    @if (loading) {
      <div class="empty"><span class="spinner"></span> Loading sales…</div>
    } @else {
      <div class="table-wrap">
        <table>
          <thead>
            <tr>
              <th>Sale #</th><th>Date</th><th>Customer</th><th>Branch</th>
              <th>Items</th><th class="mono">Total</th><th>Status</th><th></th>
            </tr>
          </thead>
          <tbody>
            @for (sale of sales; track sale.id) {
              <tr>
                <td><strong>{{ sale.saleNumber }}</strong></td>
                <td>{{ sale.saleDate | date: 'mediumDate' }}</td>
                <td>{{ sale.customer.name }}</td>
                <td>{{ sale.branch.name }}</td>
                <td>{{ activeItems(sale) }}</td>
                <td class="mono">{{ sale.totalAmount | currency }}</td>
                <td>
                  <span class="badge" [class.active]="!sale.isCancelled" [class.cancelled]="sale.isCancelled">
                    {{ sale.isCancelled ? 'Cancelled' : 'Active' }}
                  </span>
                </td>
                <td style="text-align:right;">
                  @if (!sale.isCancelled) {
                    <button class="danger sm" (click)="cancel(sale)">Cancel</button>
                  }
                </td>
              </tr>
            } @empty {
              <tr><td colspan="8"><div class="empty">No sales found. Try a different filter or create one.</div></td></tr>
            }
          </tbody>
        </table>
      </div>
      <div class="summary">
        <span>Showing <b>{{ sales.length }}</b> of <b>{{ totalItems }}</b> sales</span>
      </div>
    }
  `,
})
export class SalesListComponent implements OnInit {
  sales: Sale[] = [];
  totalItems = 0;
  customerFilter = '';
  error = '';
  loading = false;

  constructor(private readonly salesService: SalesService, private readonly router: Router) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.error = '';
    this.loading = true;
    this.salesService.list(1, 20, this.customerFilter || undefined).subscribe({
      next: (page) => {
        this.sales = page.data;
        this.totalItems = page.totalItems;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        if (err?.status === 401) {
          this.router.navigate(['/login']);
        } else {
          this.error = 'Failed to load sales.';
        }
      },
    });
  }

  activeItems(sale: Sale): number {
    return sale.items.filter((i) => !i.isCancelled).length;
  }

  cancel(sale: Sale): void {
    this.salesService.cancel(sale.id).subscribe({
      next: (updated) => Object.assign(sale, updated),
      error: () => (this.error = 'Failed to cancel sale.'),
    });
  }
}
