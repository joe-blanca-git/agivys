import { Routes } from '@angular/router';
import { SetupAppComponent } from './setup.app.component';
import { AccountComponent } from './pages/account/account.component';
import { AuthGuardService } from '../../../core/guards/auth.guard.ts.service';

export const SETUP_ROUTES: Routes = [
  {
    path: '',
    component: SetupAppComponent,
    children: [
      {
        path: 'account',
        component: AccountComponent,
        canActivate: [AuthGuardService]
      }
    ],
  },

];