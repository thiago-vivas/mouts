import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { SalesService } from '../../src/frontend/src/app/core/sales.service';
import { environment } from '../../src/frontend/src/environments/environment';
import { PaginatedResponse, Sale } from '../../src/frontend/src/app/models/sale.model';

describe('SalesService', () => {
  let service: SalesService;
  let httpMock: HttpTestingController;
  const base = `${environment.apiBaseUrl}/sales`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [SalesService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(SalesService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('lists sales with pagination params', () => {
    // Arrange
    let result: PaginatedResponse<Sale> | undefined;
    // Act
    service.list(2, 5).subscribe((r) => (result = r));

    // Assert
    const req = httpMock.expectOne((r) => r.url === base);
    expect(req.request.params.get('_page')).toBe('2');
    expect(req.request.params.get('_size')).toBe('5');

    // Act
    req.flush({ data: [], totalItems: 0, totalCount: 0, currentPage: 2, totalPages: 0 });
    // Assert
    expect(result?.currentPage).toBe(2);
  });

  it('appends a "*" wildcard to the customerName filter (prefix match)', () => {
    // Act
    service.list(1, 10, 'john').subscribe();
    // Assert
    const req = httpMock.expectOne((r) => r.url === base);
    expect(req.request.params.get('customerName')).toBe('john*');
    // Act
    req.flush({ data: [], totalItems: 0, totalCount: 0, currentPage: 1, totalPages: 0 });
  });

  it('creates a sale via POST and unwraps the data envelope', () => {
    // Arrange
    let created: Sale | undefined;
    // Act
    service
      .create({ saleDate: '2026-01-01', customer: { id: 'c', name: 'C' }, branch: { id: 'b', name: 'B' }, items: [] })
      .subscribe((s) => (created = s));

    // Assert
    const req = httpMock.expectOne(base);
    expect(req.request.method).toBe('POST');
    // Act
    req.flush({ success: true, message: 'ok', data: { id: 'new', saleNumber: 'S-1' } as Sale });
    // Assert
    expect(created?.id).toBe('new');
  });

  it('cancels a sale via PATCH', () => {
    // Arrange
    let result: Sale | undefined;
    // Act
    service.cancel('abc').subscribe((s) => (result = s));

    // Assert
    const req = httpMock.expectOne(`${base}/abc/cancel`);
    expect(req.request.method).toBe('PATCH');
    // Act
    req.flush({ success: true, message: 'ok', data: { id: 'abc', isCancelled: true } as Sale });
    // Assert
    expect(result?.isCancelled).toBeTrue();
  });
});
