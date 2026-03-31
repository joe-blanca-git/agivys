import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Plan {
  id: string;
  title: string;
  users: number;
  price: number;
  features: { text: string; included: boolean; subFeatures?: { text: string; included: boolean }[] }[];
}

@Component({
  selector: 'app-register-plan-form',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './register-plan-form.component.html',
  styleUrl: './register-plan-form.component.scss',
})
export class RegisterPlanFormComponent {
  selectedPlan: Plan | null = null;

  plans: Plan[] = [
    {
      id: 'geo-basic',
      title: 'GEO BASIC',
      users: 1,
      price: 99.0,
      features: [
        { text: 'Converter ShapeFile para formato JD', included: true },
        { text: 'Converter Shapefile para GEOJson', included: true },
        { text: 'Cadastro de Fazendas', included: true },
        {
          text: 'Integração com Operations Center',
          included: false,
          subFeatures: [
            { text: 'Cadastro de Fazendas', included: false },
            { text: 'Gestão de Combustível', included: false },
          ]
        }
      ]
    },
    {
      id: 'integrated',
      title: 'INTEGRATED',
      users: 5,
      price: 499.0,
      features: [
        { text: 'Exportar ShapeFile para monitores JD', included: true },
        { text: 'Converter Shapefile para GEOJson', included: true },
        { text: 'Cadastro de Fazendas', included: true },
        {
          text: 'Integração com Operations Center',
          included: true,
          subFeatures: [
            { text: 'Cadastro de Fazendas', included: true },
            { text: 'Gestão de Combustível', included: true },
            { text: 'Alertas de Máquina', included: false },
          ]
        }
      ]
    },
    {
      id: 'professional',
      title: 'PROFESSIONAL',
      users: 99,
      price: 899.0,
      features: [
        { text: 'Exportar ShapeFile para monitores JD', included: true },
        { text: 'Converter Shapefile para GEOJson', included: true },
        { text: 'Cadastro de Fazendas', included: true },
        {
          text: 'Integração com Operations Center',
          included: true,
          subFeatures: [
            { text: 'Cadastro de Fazendas', included: true },
            { text: 'Gestão de Equipamentos', included: true },
            { text: 'Plano de Trabalho', included: true },
          ]
        }
      ]
    }
  ];

  selectPlan(plan: Plan) {
    this.selectedPlan = plan;
  }
}