import { Routes } from '@angular/router';

export const routes: Routes = [
  //modulos
  {
    path: 'auth',
    loadChildren: () =>
      import('./features/auth/auth.routes').then((r) => r.AUTH_ROUTES),
  },
  {
    path: 'setup',
    loadChildren: () =>
      import('./features/modules/setup/setup.app.route').then((r) => r.SETUP_ROUTES),
  },
  {
    path: 'home',
    loadChildren: () =>
      import('./features/modules/home/home.route').then((r) => r.HOME_ROUTES),
  },
  {
    path: 'financial',
    loadChildren: () =>
      import('./features/modules/financial/financial.route').then((r) => r.FINANCIAL_ROUTES),
  },
  {
    path: '**',
    redirectTo: 'home',
  },
];
