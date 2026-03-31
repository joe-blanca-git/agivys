import { Component } from '@angular/core';

interface SolutionInfo {
  id: string;
  title: string;
  logo: string;
  subtitle: string;
  features: string[];
}

@Component({
  selector: 'app-register-plan-form',
  standalone: true,
  imports: [],
  templateUrl: './register-plan-form.component.html',
  styleUrl: './register-plan-form.component.scss',
})
export class RegisterPlanFormComponent {
  selectedApp: string | null = null;

  solutions: SolutionInfo[] = [
    {
      id: 'vystra',
      title: 'Vystra ERP',
      logo: 'images/logo/logo-vystra.png',
      subtitle: 'Gestão completa da sua empresa em um único sistema.',
      features: [
        'Controle financeiro completo',
        'Gestão de vendas e emissão de pedidos',
        'Controle de compras e fornecedores',
        'Gestão de estoque inteligente',
        'Integração com e-commerce e aplicativos',
        'Relatórios e indicadores em tempo real',
      ],
    },
    {
      id: 'agrovys',
      title: 'AgroVys',
      logo: 'images/logo/logo-agrovys.png',
      subtitle: 'Plataforma inteligente para gestão e integração agrícola.',
      features: [
        'Integração com Operations Center e outras plataformas',
        'Gestão de fazendas e talhões',
        'Monitoramento de máquinas em tempo real',
        'Gestão de ordens de serviço agrícolas',
        'Controle de combustível por equipamento',
        'Alertas inteligentes para falhas',
      ],
    },
  ];

  ngOnInit(): void {
    this.selectOption('agrovys');
  }

  selectOption(option: string) {
    this.selectedApp = this.selectedApp === option ? null : option;
  }
}
