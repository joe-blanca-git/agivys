import { Routes } from '@angular/router';
import { FinancialAppComponent } from './financial.app.component';
import { IncomeComponent } from './pages/income/index/income.component';
import { AuthGuardService } from '../../../core/guards/auth.guard.ts.service';
import { ExpensesComponent } from './pages/expenses/index/expenses.component';
import { AccountReceiveComponent } from './pages/account-receive/index/account-receive.component';
import { AccountPayComponent } from './pages/account-pay/index/account-pay.component';

export const FINANCIAL_ROUTES: Routes = [
  {
    path: '',
    component: FinancialAppComponent,
    children: [
      {
        path: 'income',
        component: IncomeComponent,
        canActivate: [AuthGuardService],
      },
      {
        path: 'expense',
        component: ExpensesComponent,
        canActivate: [AuthGuardService],
      },
      {
        path: 'account-pay',
        component: AccountPayComponent,
        canActivate: [AuthGuardService],
      },
      {
        path: 'account-receive',
        component: AccountReceiveComponent,
        canActivate: [AuthGuardService],
      },

    ],
  },
];
