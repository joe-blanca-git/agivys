import { Component, OnInit } from '@angular/core';
import {
  startOfMonth,
  format,
  isWithinInterval,
  startOfDay,
  endOfDay,
  subMonths,
  startOfYear,
  endOfYear,
} from 'date-fns';
import { FinancialService } from '../../../services/financial.service';
import { CommonModule } from '@angular/common';
import { FormatPipe } from '../../../../../../core/pipes/format.pipe';
import { lastValueFrom } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { NzDatePickerModule } from 'ng-zorro-antd/date-picker';
import { NzInputModule } from 'ng-zorro-antd/input';
import { NzSpinModule } from 'ng-zorro-antd/spin';
import { NzTableModule } from 'ng-zorro-antd/table';
import { NzToolTipModule } from 'ng-zorro-antd/tooltip';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzTagModule } from 'ng-zorro-antd/tag';
import { PayFinancialComponent } from '../../../components/pay-financial/pay-financial.component';

@Component({
  selector: 'app-account-receive',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    NzTableModule,
    FormatPipe,
    NzDatePickerModule,
    NzInputModule,
    NzSpinModule,
    NzToolTipModule,
    NzButtonModule,
    NzTagModule,
    PayFinancialComponent,
  ],
  templateUrl: './account-receive.component.html',
  styleUrls: [
    './account-receive.component.scss',
    '../../../../../../app.component.scss',
  ],
})
export class AccountReceiveComponent implements OnInit {
  title = 'Contas a Receber';
  description = 'Gestão de faturamento e parcelas pendentes.';

  dateRangeApi: Date[] = [startOfMonth(new Date()), endOfDay(new Date())];
  dateRangeFilter: Date[] | null = null;

  isLoadingData = false;
  searchTerm = '';

  isVisiblePayFinancial:boolean = false;

  originalInstallments: any[] = [];
  filteredInstallments: any[] = [];
  selectedInstallments: any = null;

  dashboardCards = [
    {
      title: 'Total Geral',
      value: 0,
      icon: 'fa-solid fa-sack-dollar text-success',
      bg: 'bg-success-subtle',
    },
    {
      title: 'Pendente',
      value: 0,
      icon: 'fa-solid fa-clock text-warning',
      bg: 'bg-warning-subtle',
    },
    {
      title: 'Vencido',
      value: 0,
      icon: 'fa-solid fa-exclamation-circle text-danger',
      bg: 'bg-danger-subtle',
    },
  ];

  ranges = {
    Hoje: [new Date(), new Date()],
    'Este Mês': [startOfMonth(new Date()), endOfDay(new Date())],
    'Últimos 3 Meses': [
      subMonths(startOfMonth(new Date()), 2),
      endOfDay(new Date()),
    ],
    'Este Ano': [startOfYear(new Date()), endOfYear(new Date())],
  };

  listOfColumn = [
    { title: 'Parcela', compare: (a: any, b: any) => a.number - b.number },
    {
      title: 'Vencimento',
      compare: (a: any, b: any) =>
        (a.dueDate || '').localeCompare(b.dueDate || ''),
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
      title: 'Status',
      compare: (a: any, b: any) =>
        (a.status || '').localeCompare(b.status || ''),
    },
    { title: 'Valor', compare: (a: any, b: any) => a.value - b.value },
  ];

  constructor(private financialService: FinancialService) {}

  ngOnInit(): void {
    this.loadData();
  }

  async loadData() {
    this.isLoadingData = true;
    const filters = {
      StartDate: format(this.dateRangeApi[0], 'yyyy-MM-dd'),
      EndDate: format(this.dateRangeApi[1], 'yyyy-MM-dd'),
      Type: 1,
    };

    try {
      const result = await lastValueFrom(
        this.financialService.getFinancialMov(filters),
      );
      const movements = result.filter((i: any) => i.status !== 'PAID') || [];

      this.originalInstallments = movements.flatMap((mov: any) =>
        mov.installments
          .filter((p: any) => p.status !== 'PAID')
          .map((p: any) => ({
            ...p,
            description: mov.description,
            personName: mov.personName,
            categoryName: mov.categoryName,
            totalInstallments: mov.installmentsCount, // Pega o total do pai
            isOverdue:
              new Date(p.dueDate).getTime() < startOfDay(new Date()).getTime(),
          })),
      );

      this.applyFilters();
    } catch (error) {
      this.originalInstallments = [];
      this.applyFilters();
    } finally {
      this.isLoadingData = false;
    }
  }

  applyFilters() {
    let data = [...this.originalInstallments];

    if (this.searchTerm) {
      const s = this.searchTerm.toLowerCase();
      data = data.filter(
        (i) =>
          i.description?.toLowerCase().includes(s) ||
          i.personName?.toLowerCase().includes(s) ||
          i.categoryName?.toLowerCase().includes(s) ||
          i.number?.toString().includes(s),
      );
    }

    if (this.dateRangeFilter && this.dateRangeFilter.length === 2) {
      const start = startOfDay(this.dateRangeFilter[0]);
      const end = endOfDay(this.dateRangeFilter[1]);

      data = data.filter((i) => {
        const due = new Date(i.dueDate);
        return isWithinInterval(due, { start, end });
      });
    }

    this.filteredInstallments = data;
    this.updateDashboard();
  }

  updateDashboard() {
    const hoje = startOfDay(new Date()).getTime();

    this.dashboardCards[0].value = this.filteredInstallments.reduce(
      (acc, curr) => acc + (curr.value || 0),
      0,
    );

    this.dashboardCards[1].value = this.filteredInstallments
      .filter((p) => new Date(p.dueDate).getTime() >= hoje)
      .reduce((acc, curr) => acc + (curr.value || 0), 0);

    this.dashboardCards[2].value = this.filteredInstallments
      .filter((p) => new Date(p.dueDate).getTime() < hoje)
      .reduce((acc, curr) => acc + (curr.value || 0), 0);
  }

  getStatusColor(status: string, isOverdue: boolean): string {
    if (isOverdue) return 'error';
    if (status === 'Open') return 'warning';
    return 'default';
  }

  onPayFinancial(financial: any){
  this.selectedInstallments = financial;
  this.isVisiblePayFinancial = true;
}

  onPayFinancialStatusChange(){
    this.isVisiblePayFinancial = !this.isVisiblePayFinancial
  }
}
