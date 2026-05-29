import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { SalesListComponent } from '../../src/frontend/src/app/pages/sales-list.component';
import { environment } from '../../src/frontend/src/environments/environment';

describe('SalesListComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SalesListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads sales on init and exposes the total', () => {
    // Arrange
    const fixture = TestBed.createComponent(SalesListComponent);
    fixture.detectChanges(); // triggers ngOnInit

    // Assert
    const req = httpMock.expectOne((r) => r.url === `${environment.apiBaseUrl}/sales`);
    // Act
    req.flush({
      data: [
        {
          id: '1', saleNumber: 'S-1', saleDate: '2026-01-01',
          customer: { id: 'c', name: 'C' }, branch: { id: 'b', name: 'B' },
          totalAmount: 10, isCancelled: false, items: [],
        },
      ],
      totalItems: 1, totalCount: 1, currentPage: 1, totalPages: 1,
    });

    // Assert
    expect(fixture.componentInstance.sales.length).toBe(1);
    expect(fixture.componentInstance.totalItems).toBe(1);
  });
});
