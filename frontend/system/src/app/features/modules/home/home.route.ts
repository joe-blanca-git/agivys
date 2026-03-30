import { Routes } from '@angular/router';
import { HomeAppComponent } from './home.app.component';
import { AuthGuardService } from '../../../core/guards/auth.guard.ts.service';

export const HOME_ROUTES: Routes = [
  {
    path: '',
    component: HomeAppComponent,
    canActivate: [AuthGuardService],
    children: [
    ],
  },

];