export interface ExternalRef {
  id: string;
  name: string;
}

export interface SaleItem {
  id: string;
  product: ExternalRef;
  quantity: number;
  unitPrice: number;
  discount: number;
  totalAmount: number;
  isCancelled: boolean;
}

export interface Sale {
  id: string;
  saleNumber: string;
  saleDate: string;
  customer: ExternalRef;
  branch: ExternalRef;
  totalAmount: number;
  isCancelled: boolean;
  items: SaleItem[];
}

export interface CreateSaleItem {
  productId: string;
  productName: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateSale {
  saleDate: string;
  customer: ExternalRef;
  branch: ExternalRef;
  items: CreateSaleItem[];
}

/** The backend envelope: { success, message, data }. */
export interface ApiResponse<T> {
  success: boolean;
  message: string;
  data: T;
}

/** Paginated list envelope (general-api.md): { data, totalItems, currentPage, totalPages }. */
export interface PaginatedResponse<T> {
  data: T[];
  totalItems: number;
  totalCount: number;
  currentPage: number;
  totalPages: number;
}
