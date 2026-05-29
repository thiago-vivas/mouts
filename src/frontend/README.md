# DeveloperStore — Angular frontend

A minimal Angular 18 (standalone) SPA that consumes the Sales API: login,
list/filter sales, create a sale (with live discount rules on the backend),
and cancel a sale.

## Prerequisites
- Node.js 18+ and npm
- The backend running (see `../backend`). Default API URL is
  `http://localhost:8080/api` — adjust `src/environments/environment.ts` to match
  where the backend listens.

## Install, run, test
```bash
cd src/frontend
npm install
npm start        # ng serve → http://localhost:4200
npm run build    # production build → dist/
npm test         # Karma + Jasmine unit tests (headless Chrome)
```
The Jasmine specs live at the repo root under `tests/frontend/` (not co-located
in `src/`); `tsconfig.spec.json` points the test build at them. `npm test` runs them.

## Login
The backend seeds a default user (in non-production): **admin@developerstore.com / Admin@123**.

## Structure
```
src/app/
├── core/         # AuthService, SalesService, JWT auth interceptor, auth route guard
├── models/       # Sale / CreateSale / API envelope types
├── pages/        # login, sales-list, sale-form components
├── app.config.ts # provideHttpClient(withInterceptors([authInterceptor])) + router
└── app.routes.ts # /sales and /sales/new protected by authGuard
```

## Notes
- The JWT from `/api/auth` is stored in `localStorage` and attached to every API
  request by `authInterceptor`.
- The backend enables CORS for `http://localhost:4200` (configurable via
  `Cors:AllowedOrigins`).
- Tests use `HttpTestingController` to assert request shapes (pagination params,
  the `*` wildcard filter, the `{ data }` envelope unwrapping) without a live API.
