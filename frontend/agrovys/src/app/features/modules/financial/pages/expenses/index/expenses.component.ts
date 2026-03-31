import { Component } from '@angular/core';
import { FinancialService } from '../../../services/financial.service';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTableModule } from 'ng-zorro-antd/table';
import { startOfMonth, format } from 'date-fns';
import { FormatPipe } from '../../../../../../core/pipes/format.pipe';
import { NewFinancialMovComponent } from '../../../components/new-financial-mov/new-financial-mov.component';
import { lastValueFrom } from 'rxjs';

@Component({
  selector: 'app-expenses',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzTableModule,
    FormatPipe,
    NzDatePickerModule,
    NzInputModule,
    NzSpinModule,
    NewFinancialMovComponent,
  ],
  templateUrl: './expenses.component.html',
  styleUrls: [
    './expenses.component.scss',
    '../../../../../../app.component.scss',
  ],
})
export class ExpensesComponent {
  title = 'Despesas';
  description = 'Gestão de movimentações do tipo saída.';

  dateRange: Date[] = [startOfMonth(new Date()), new Date()];
  listOfData: any[] = [];
  filteredData: any[] = [];
  isLoadingData = false;
  newFinancialMoveVisible = false;

  dashboardCards = [
    {
      title: 'Total',
      value: 0,
      icon: 'fa-solid fa-sack-dollar text-danger',
      bg: 'bg-danger-subtle',
    },
    {
      title: 'Pendente',
      value: 0,
      icon: 'fa-solid fa-hand-holding-dollar text-warning',
      bg: 'bg-warning-subtle',
    },
  ];

  ranges = {
    Hoje: [new Date(), new Date()],
    'Mês Atual': [startOfMonth(new Date()), new Date()],
  };

  listOfColumn = [
    {
      title: 'Lançamento',
      compare: (a: any, b: any) =>
        (a.documentDate || '').localeCompare(b.documentDate || ''),
    },
    {
      title: 'Vencimento',
      compare: (a: any, b: any) =>
        (a.installments?.[0]?.dueDate || '').localeCompare(
          b.installments?.[0]?.dueDate || '',
        ),
    },
    {
      title: 'Origem',
      compare: (a: any, b: any) =>
        (a.originDescription || '').localeCompare(b.originDescription || ''),
    },
    {
      title: 'Descrição',
      compare: (a: any, b: any) =>
        (a.description || '').localeCompare(b.description || ''),
    },
    {
      title: 'Pessoa',
      compare: (a: any, b: any) =>
        (a.personName || '').localeCompare(b.personName || ''),
    },
    {
      title: 'Categoria',
      compare: (a: any, b: any) =>
        (a.categoryName || '').localeCompare(b.categoryName || ''),
    },
    {
      title: 'Valor',
      compare: (a: any, b: any) => a.totalAmount - b.totalAmount,
    },
    {
      title: 'Pendente',
      compare: (a: any, b: any) =>
        this.calculatePending(a) - this.calculatePending(b),
    },
  ];

  constructor(private financialService: FinancialService) {}

  ngOnInit(): void {
    this.loadData();
  }

  async loadData() {
    this.isLoadingData = true;
    const filters = {
      StartDate: format(this.dateRange[0], 'yyyy-MM-dd'),
      EndDate: format(this.dateRange[1], 'yyyy-MM-dd'),
      Type: 2,
    };

    try {
      const result = await lastValueFrom(
        this.financialService.getFinancialMov(filters),
      );
      this.listOfData = result || [];
      this.filteredData = [...this.listOfData];
      this.updateDashboard();
    } catch (error) {
      this.listOfData = [];
      this.filteredData = [];
    } finally {
      this.isLoadingData = false;
    }
  }

  onSearch(event: any) {
    const value = (event.target as HTMLInputElement).value.toLowerCase();
    if (!value) {
      this.filteredData = [...this.listOfData];
      return;
    }
    this.filteredData = this.listOfData.filter((item) =>
      Object.values(item).some((val) =>
        String(val).toLowerCase().includes(value),
      ),
    );
  }

  updateDashboard() {
    this.dashboardCards[0].value = this.listOfData.reduce(
      (acc, curr) => acc + (curr.totalAmount || 0),
      0,
    );

    this.dashboardCards[1].value = this.listOfData.reduce(
      (acc, curr) => acc + this.calculatePending(curr),
      0,
    );
  }

  newFinancialMovStatusChange() {
    this.newFinancialMoveVisible = !this.newFinancialMoveVisible;
  }

  async onCreatedFinancialmov(mov: any) {
    if (mov?.id) {
      await this.loadData();
      this.newFinancialMoveVisible = false;
    }
  }

  calculatePending(item: any): number {
    if (!item.installments) return 0;
    return item.installments
      .filter((i: any) => i.status !== 'PAID')
      .reduce((acc: number, curr: any) => acc + (curr.value || 0), 0);
  }
}
