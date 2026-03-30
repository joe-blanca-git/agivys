import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';

interface SolutionInfo {
  id: string;
  title: string;
  logo: string;
  subtitle: string;
  features: string[];
}

@Component({
  selector: 'app-register-company-form',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './register-company-form.component.html',
  styleUrl: './register-company-form.component.scss',
})
export class RegisterCompanyFormComponent {

  selected: string | null = null;

  solutions: SolutionInfo[] = [
    {
      id: 'vystra',
      title: 'Vystra ERP',
      logo: 'images/logo/logo-vystra.png',
      subtitle: 'Gestão completa da sua empresa em um único sistema.',
      features: [
        'Controle financeiro completo (receitas, despesas e fluxo de caixa)',
        'Gestão de vendas e emissão de pedidos',
        'Controle de compras e fornecedores',
        'Gestão de estoque inteligente',
        'Integração com e-commerce e aplicativos',
        'Relatórios e indicadores em tempo real'
      ]
    },
    {
      id: 'agrovys',
      title: 'AgroVys',
      logo: 'images/logo/logo-agrovys.png',
      subtitle: 'Plataforma inteligente para gestão e integração agrícola.',
      features: [
        'Integração com Operations Center e outras plataformas agrícolas',
        'Gestão de fazendas e talhões',
        'Monitoramento de máquinas em tempo real',
        'Gestão de ordens de serviço agrícolas',
        'Controle de combustível por equipamento',
        'Alertas inteligentes para falhas e manutenção'
      ]
    }
  ];

  selectOption(option: string) {
    this.selected = this.selected === option ? null : option;
  }

  get selectedSolution(): SolutionInfo | null {
    return this.solutions.find(s => s.id === this.selected) || null;
  }

}