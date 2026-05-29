import { Routes } from '@angular/router';
import { authGuard } from './core/auth.guard';
import { LoginComponent } from './pages/login.component';
import { SalesListComponent } from './pages/sales-list.component';
import { SaleFormComponent } from './pages/sale-form.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'sales' },
  { path: 'login', component: LoginComponent },
  { path: 'sales', component: SalesListComponent, canActivate: [authGuard] },
  { path: 'sales/new', component: SaleFormComponent, canActivate: [authGuard] },
  { path: '**', redirectTo: 'sales' },
];
