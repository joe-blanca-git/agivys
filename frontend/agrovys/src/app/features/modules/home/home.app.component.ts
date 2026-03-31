import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

@Component({
  selector: 'home-app-root',
  templateUrl: './home.app.component.html',
  styleUrls: ['./home.app.component.scss', '../../../app.component.scss'],
  standalone: true,
  imports: [CommonModule],
})
export class HomeAppComponent {
  title = 'Seja Bem Vindo!';
  description = 'Funcionalidades mais acessadas...';

  mostAccessed: any[] = [
    {
      title: 'Pagar/Receber',
      description: 'Gestão de Contas a Pagar e Contas a Receber',
      route: '/finance/accounts',
      icon: 'fa-solid fa-money-bill-transfer',
    },
    {
      title: 'Vendas',
      description: 'Gestão de Pedidos de Vendas',
      route: '/orders/sales',
      icon: 'fa fa-shopping-cart',
    },
    {
      title: 'Compras',
      description: 'Gestão de Pedidos de Compras',
      route: '/orders/purchase',
      icon: 'fa-solid fa-briefcase',
    },
    {
      title: 'Estoque',
      description: 'Gestão de Estoque',
      route: '/orders/purchase',
      icon: 'fa-solid fa-dolly',
    },
  ];
}
