import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule, RouterOutlet } from '@angular/router';

@Component({
  selector: 'financial-app-root',
  templateUrl: './financial.app.component.html',
  styleUrls: ['./financial.app.component.scss', '../../../app.component.scss'],
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterModule,],
})
export class FinancialAppComponent {
  title = 'Financeiro';
  description = 'Gestão de Receitas, Despesas, Pagamentos e Transferências.';

}
