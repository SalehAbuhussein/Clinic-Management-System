# Frontend

The Angular 22 client for the Clinic Management System. See the
[root README](../README.md) for what the app does and how to run both halves together.

```
npm install
npm start      # http://localhost:4200, proxies /api to https://localhost:7180
npm run build
```

`proxy.conf.json` is what makes `/api` same-origin in development. Without it the
browser hides `Content-Disposition` from script and Excel exports lose their filename.

## How it is put together

- `core/` holds the API service, the auth service and the two interceptors. One attaches
  the bearer token and retries once through a shared refresh; the other flattens every
  failure into an `ApiError`.
- `shared/` holds services, the dashboard layout and the components screens reuse:
  pagination, the export button, the date range picker, the confirm dialog.
- `features/` is one folder per screen, each with its own `components/` and `types/`,
  lazily loaded by its own routes file.

State is signals throughout. Lists use `httpResource` through `getPagedResource`, so a
screen declares its filters and the request follows. Forms use Angular's signal forms.
