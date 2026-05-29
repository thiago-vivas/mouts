import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { ApiResponse } from '../models/sale.model';

interface AuthData {
  token: string;
  email: string;
  name: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly tokenKey = 'auth_token';
  private readonly nameKey = 'auth_name';

  constructor(private readonly http: HttpClient) {}

  /** POST /api/auth { email, password } → token (token + display name stored locally). */
  login(email: string, password: string): Observable<string> {
    return this.http
      .post<ApiResponse<AuthData>>(`${environment.apiBaseUrl}/auth`, { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(this.tokenKey, response.data.token);
          localStorage.setItem(this.nameKey, response.data.name || response.data.email || 'Account');
        }),
        map((response) => response.data.token),
      );
  }

  logout(): void {
    localStorage.removeItem(this.tokenKey);
    localStorage.removeItem(this.nameKey);
  }

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  get displayName(): string {
    return localStorage.getItem(this.nameKey) ?? 'Account';
  }

  get isAuthenticated(): boolean {
    return !!this.token;
  }
}
