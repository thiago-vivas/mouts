import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet, Router } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <header class="app-header">
      <div class="inner">
        <span class="brand"><span class="dot"></span> Thiago Store</span>
        @if (auth.isAuthenticated) {
          <nav class="nav-links">
            <a routerLink="/sales" routerLinkActive="active">Sales</a>
            <a routerLink="/sales/new" routerLinkActive="active">New sale</a>
          </nav>
        }
        <span class="spacer"></span>
        @if (auth.isAuthenticated) {
          <span class="user-chip">
            <span class="avatar">{{ initials }}</span>
            {{ auth.displayName }}
          </span>
          <button class="ghost sm" (click)="logout()">Logout</button>
        } @else {
          <a routerLink="/login"><button class="sm">Login</button></a>
        }
      </div>
    </header>
    <main>
      <router-outlet />
    </main>
  `,
})
export class AppComponent {
  constructor(public auth: AuthService, private router: Router) {}

  get initials(): string {
    return this.auth.displayName.trim().charAt(0).toUpperCase() || 'A';
  }

  logout(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
