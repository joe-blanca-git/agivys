import { Routes } from '@angular/router';
import { FarmsAppComponent } from './features/modules/farms/farms.app.component';
import { AuthGuardService } from './core/guards/auth.guard.ts.service';

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
    path: 'farms',
    component: FarmsAppComponent,
    canActivate: [AuthGuardService],
  },
  {
    path: '**',
    redirectTo: 'home',
  },
];
