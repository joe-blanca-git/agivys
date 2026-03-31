import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { NzTableModule } from 'ng-zorro-antd/table';
import { FormatPipe } from '../../../../../core/pipes/format.pipe';

interface IncomeItem {
  dueDate: string;
  payDate: string;
  description: string;
  amount: number;
  method: string;
  status: string;
}

@Component({
  selector: 'app-account',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, NzTableModule, FormatPipe],
  templateUrl: './account.component.html',
  styleUrls: ['./account.component.scss', '../../../../../app.component.scss'],
})
export class AccountComponent {
  title = 'Minha Conta';
  description =
    'Informações cadastrais, histórico de pagamentos e gestão de usuários.';

  listOfColumn = [
    {
      title: 'Vencimento',
      compare: (a: IncomeItem, b: IncomeItem) =>
        a.dueDate.localeCompare(b.dueDate),
      priority: false,
    },
    {
      title: 'Pagamento',
      compare: (a: IncomeItem, b: IncomeItem) =>
        a.payDate.localeCompare(b.payDate),
      priority: false,
    },
    {
      title: 'Descrição',
      compare: (a: IncomeItem, b: IncomeItem) =>
        a.description.localeCompare(b.description),
      priority: false,
    },
    {
      title: 'Valor',
      compare: (a: IncomeItem, b: IncomeItem) => a.amount - b.amount,
      priority: 3,
    },
    {
      title: 'Forma Pagamento',
      compare: (a: IncomeItem, b: IncomeItem) =>
        a.method.localeCompare(b.method),
      priority: false,
    },
    {
      title: 'Status',
      compare: (a: IncomeItem, b: IncomeItem) =>
        a.status.localeCompare(b.status),
      priority: false,
    },
  ];

  listOfData: IncomeItem[] = [
    {
      payDate: '2024-01-01',
      dueDate: '2024-01-05',
      description: 'January Salary',
      amount: 5500,
      method: 'Credit Card',
      status: 'Pago',
    },
    {
      payDate: '2024-01-01',
      dueDate: '2024-01-05',
      description: 'January Salary',
      amount: 5500,
      method: 'Credit Card',
      status: 'Pago',
    },
    {
      payDate: '2024-01-01',
      dueDate: '2024-01-05',
      description: 'January Salary',
      amount: 5500,
      method: 'Pix Card',
      status: 'Pending',
    },
  ];

  user: any = {
    name: 'Jhon Doe',
    document: '393.955.338.70',
    birth: '19/03/1990',
    type: 'ADMIN',
    position: 'Gerente',
    contact: {
      phonePrimary: '1638390000',
      phoneSecondary: '16988766522',
      emailPrimary: 'jhondoe@email.com',
      emailSecondary: 'johndoe2@email.com',
    },
    address: {
      zip: '14500000',
      street: 'Rua Geronimo da Silva',
      number: '300',
      complement: 'apto 10',
      city: 'New York',
      neighborhood: 'Centro',
    },
  };

  trackColumn(_: number, column: any) {
    return column.title;
  }

  trackData(_: number, data: IncomeItem) {
    return data.dueDate + data.description;
  }
}
