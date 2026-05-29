import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule],
  template: `
    <div style="display:grid; place-items:center; padding-top: 6vh;">
      <div class="card" style="max-width: 380px; width: 100%;">
        <h2>Welcome back</h2>
        <p style="color: var(--muted); margin: 0 0 4px;">Sign in to manage sales.</p>

        <form (ngSubmit)="submit()">
          <label for="email">Email</label>
          <input id="email" type="email" autocomplete="username" [(ngModel)]="email" name="email" />
          <label for="password">Password</label>
          <input id="password" type="password" autocomplete="current-password" [(ngModel)]="password" name="password" />

          <button type="submit" [disabled]="loading" style="width:100%; justify-content:center; margin-top:18px;">
            @if (loading) { <span class="spinner"></span> Signing in… } @else { Sign in }
          </button>
        </form>

        @if (error) { <div class="alert error">{{ error }}</div> }

        <p style="margin: 16px 0 0; text-align:center;">
          <small>Demo user — admin&#64;developerstore.com / Admin&#64;123</small>
        </p>
      </div>
    </div>
  `,
})
export class LoginComponent {
  email = 'admin@developerstore.com';
  password = 'Admin@123';
  loading = false;
  error = '';

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  submit(): void {
    this.loading = true;
    this.error = '';
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.router.navigate(['/sales']),
      error: () => {
        this.error = 'Invalid credentials. Please try again.';
        this.loading = false;
      },
    });
  }
}
