import { CurrencyPipe } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { SalesService } from '../core/sales.service';
import { CreateSale, CreateSaleItem } from '../models/sale.model';

@Component({
  selector: 'app-sale-form',
  standalone: true,
  imports: [FormsModule, RouterLink, CurrencyPipe],
  template: `
    <div class="page-head">
      <div>
        <h2>New sale</h2>
        <p>Discounts are applied automatically by the server: 5–9 → 10%, 10–20 → 20% (max 20 per item).</p>
      </div>
      <a routerLink="/sales"><button class="secondary">Back</button></a>
    </div>

    <div class="card">
      <div class="field-row">
        <div>
          <label for="customer">Customer</label>
          <input id="customer" placeholder="Customer name" [(ngModel)]="customerName" name="customer" />
        </div>
        <div>
          <label for="branch">Branch</label>
          <input id="branch" placeholder="Branch name" [(ngModel)]="branchName" name="branch" />
        </div>
      </div>

      <h3 style="margin-top: 22px;">Items</h3>
      <div class="table-wrap" style="margin-top: 8px;">
        <table>
          <thead>
            <tr><th style="width:40%">Product</th><th>Qty</th><th>Unit price</th><th>Discount</th><th class="mono">Subtotal</th><th></th></tr>
          </thead>
          <tbody>
            @for (item of items; track $index) {
              <tr>
                <td><input placeholder="Product name" [(ngModel)]="item.productName" name="p{{ $index }}" /></td>
                <td><input type="number" min="1" max="20" [(ngModel)]="item.quantity" name="q{{ $index }}" style="width:80px" /></td>
                <td><input type="number" min="0.01" step="0.01" [(ngModel)]="item.unitPrice" name="u{{ $index }}" style="width:110px" /></td>
                <td>
                  @if (rate(item.quantity) > 0) {
                    <span class="badge active">{{ ratePercent(item.quantity) }}%</span>
                  } @else if (item.quantity > 20) {
                    <span class="badge cancelled">over 20</span>
                  } @else { — }
                </td>
                <td class="mono">{{ subtotal(item) | currency }}</td>
                <td style="text-align:right;"><button class="ghost sm" aria-label="Remove item" (click)="removeItem($index)" [disabled]="items.length === 1">✕</button></td>
              </tr>
            }
          </tbody>
        </table>
      </div>
      <p style="margin-top:10px;"><button class="secondary sm" (click)="addItem()">+ Add item</button></p>

      @if (hasOverLimit) { <div class="alert warn">An item exceeds the 20-unit limit and will be rejected by the server.</div> }
      @if (error) { <div class="alert error">{{ error }}</div> }

      <div style="display:flex; align-items:center; justify-content:space-between; margin-top:18px;">
        <div class="summary" style="margin:0;">
          <span>Items: <b>{{ items.length }}</b></span>
          <span>Estimated total: <b>{{ total | currency }}</b></span>
        </div>
        <button (click)="submit()" [disabled]="loading || !canSubmit">
          @if (loading) { <span class="spinner"></span> Creating… } @else { Create sale }
        </button>
      </div>
    </div>
  `,
})
export class SaleFormComponent {
  customerName = '';
  branchName = '';
  items: CreateSaleItem[] = [this.newItem()];
  loading = false;
  error = '';

  constructor(private readonly salesService: SalesService, private readonly router: Router) {}

  private newItem(): CreateSaleItem {
    return { productId: crypto.randomUUID(), productName: '', quantity: 1, unitPrice: 1 };
  }

  addItem(): void {
    this.items.push(this.newItem());
  }

  removeItem(index: number): void {
    this.items.splice(index, 1);
  }

  /** Mirrors the server DiscountPolicy for a live preview. */
  rate(qty: number): number {
    if (qty < 5 || qty > 20) return 0;
    return qty >= 10 ? 0.2 : 0.1;
  }

  ratePercent(qty: number): number {
    return Math.round(this.rate(qty) * 100);
  }

  subtotal(item: CreateSaleItem): number {
    const gross = item.quantity * item.unitPrice;
    return gross - gross * this.rate(item.quantity);
  }

  get total(): number {
    return this.items.reduce((sum, item) => sum + this.subtotal(item), 0);
  }

  get hasOverLimit(): boolean {
    return this.items.some((i) => i.quantity > 20);
  }

  get canSubmit(): boolean {
    return (
      !!this.customerName.trim() &&
      !!this.branchName.trim() &&
      this.items.length > 0 &&
      this.items.every((i) => i.productName.trim() && i.quantity >= 1 && i.quantity <= 20 && i.unitPrice > 0)
    );
  }

  submit(): void {
    this.loading = true;
    this.error = '';
    const payload: CreateSale = {
      saleDate: new Date().toISOString(),
      customer: { id: crypto.randomUUID(), name: this.customerName },
      branch: { id: crypto.randomUUID(), name: this.branchName },
      items: this.items,
    };
    this.salesService.create(payload).subscribe({
      next: () => this.router.navigate(['/sales']),
      error: (err) => {
        this.error = err?.error?.detail ?? 'Failed to create sale.';
        this.loading = false;
      },
    });
  }
}
