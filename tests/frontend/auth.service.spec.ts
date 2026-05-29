import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { AuthService } from '../../src/frontend/src/app/core/auth.service';
import { environment } from '../../src/frontend/src/environments/environment';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
    localStorage.clear();
  });

  afterEach(() => httpMock.verify());

  it('posts credentials and stores the returned token', () => {
    // Arrange
    let token: string | undefined;
    // Act
    service.login('a@b.com', 'pw').subscribe((t) => (token = t));

    // Assert
    const req = httpMock.expectOne(`${environment.apiBaseUrl}/auth`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'pw' });

    // Act
    req.flush({ success: true, message: 'ok', data: { token: 'jwt-123', email: 'a@b.com', name: 'A', role: 'Admin' } });

    // Assert
    expect(token).toBe('jwt-123');
    expect(service.token).toBe('jwt-123');
    expect(service.isAuthenticated).toBeTrue();
  });

  it('clears the token on logout', () => {
    // Arrange
    localStorage.setItem('auth_token', 'x');
    // Act
    service.logout();
    // Assert
    expect(service.isAuthenticated).toBeFalse();
  });
});
