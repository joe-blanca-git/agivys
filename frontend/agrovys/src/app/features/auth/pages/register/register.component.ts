import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RegisterFormComponent } from '../../components/register-form/register-form.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { AccountService } from '../../../modules/setup/pages/account/services/account.service';
import { lastValueFrom } from 'rxjs';
import { RegisterCompanyFormComponent } from '../../components/register-company-form/register-company-form.component';
import { RegisterPlanFormComponent } from '../../components/register-plan-form/register-plan-form.component';

interface Step {
  index: number;
  name: string;
  icon: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterModule, RegisterFormComponent, RegisterCompanyFormComponent, RegisterPlanFormComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss', '../../auth.app.component.scss'],
})
export class RegisterComponent {
  @ViewChild(RegisterFormComponent) formComponent!: RegisterFormComponent;
  @ViewChild(RegisterCompanyFormComponent) formCompanyComponent!: RegisterCompanyFormComponent;
  
  currentStep = 0;
  
  // Controles Passo 0
  isPersonalDataComplete = false;
  savedPersonalData: any = null;
  
  // Controles Passo 1
  isCompanyDataComplete = false;
  savedCompanyData: any = null;

  urlAccesagrovys = '';

  steps: Step[] = [
    { index: 0, name: 'Dados Pessoais', icon: 'fa-user' },
    { index: 1, name: 'Organização', icon: 'fa-building' },
    { index: 2, name: 'Plano', icon: 'fa-money-bill' },
  ];

  constructor(
    private authService: AuthService,
    private accountService: AccountService,
  ) {}

  async processRegister(formInfo: any) {
    try {
      const resRegistro: any = await lastValueFrom(
        this.authService.register(formInfo.userBody)
      );

      if (resRegistro && (resRegistro.status === 400 || resRegistro.errors)) {
        console.error('A API recusou o cadastro:', resRegistro);
        return; 
      }

      this.savedPersonalData = formInfo.userBody;
      this.isPersonalDataComplete = true;
      this.currentStep++;

    } catch (error) {
      console.error('Falha crítica na requisição (Erro 400/500):', error);
    }
  }

  async processRegisterCompany(formInfo: any) {
    try {
      const rawData = formInfo.data;

      const companyBody = {
        name: rawData.organizationName
      };

      const addressBody = {
        description: rawData.description || 'Sede',
        zipCode: rawData.cep.replace(/\D/g, ''),
        street: rawData.street,
        number: rawData.number,
        complement: '',
        neighborhood: '',
        city: rawData.city,
        state: rawData.state
      };

      // 1. Cadastra a Empresa (ajuste o nome do método no AccountService se necessário)
      const resCompany: any = await lastValueFrom(
        this.accountService.registerCompany(companyBody)
      );

      if (resCompany && (resCompany.status === 400 || resCompany.errors)) {
        console.error('Erro ao cadastrar empresa:', resCompany);
        return; 
      }

      // 2. Cadastra o Endereço (ajuste o nome do método no AccountService se necessário)
      const resAddress: any = await lastValueFrom(
        this.accountService.registerCompanyAddress(addressBody)
      );

      if (resAddress && (resAddress.status === 400 || resAddress.errors)) {
        console.error('A API recusou o cadastro do endereço da empresa:', resAddress);
        return; 
      }

      // SUCESSO: Salva os dados para exibição (read-only) e avança
      this.savedCompanyData = rawData;
      this.isCompanyDataComplete = true;
      this.currentStep++;

    } catch (error) {
      console.error('Falha crítica na requisição de empresa (Erro 400/500):', error);
    }
  }

  async nextStep() {
    // PASSO 0: Dados Pessoais
    this.currentStep++;
    if (this.currentStep === 0) {
      if (this.isPersonalDataComplete) {
        this.currentStep++;
        return;
      }

      const formInfo = this.formComponent.getFormData();
      if (!formInfo.isValid) {
        this.formComponent.formRegister.markAllAsTouched();
        return;
      }
      
      await this.processRegister(formInfo);
      return;
    }

    // PASSO 1: Meu Negócio
    if (this.currentStep === 1) {
      if (this.isCompanyDataComplete) {
        this.currentStep++;
        return;
      }

      const formInfo = this.formCompanyComponent.getFormData();
      if (!formInfo.isValid) {
        this.formCompanyComponent.formRegisterCompany.markAllAsTouched();
        return;
      }
      
      await this.processRegisterCompany(formInfo);
      return;
    }

    // DEMAIS PASSOS
    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    }
  }

  setStep(index: number) {
    // Validação Step 0
    if (this.currentStep === 0 && index > 0) {
      if (!this.isPersonalDataComplete) {
        const formInfo = this.formComponent.getFormData();
        if (!formInfo.isValid) {
          this.formComponent.formRegister.markAllAsTouched();
          return;
        }
      }
    }

    // Validação Step 1 (Impede pular do 1 para o 2 se estiver inválido)
    if (this.currentStep === 1 && index > 1) {
      if (!this.isCompanyDataComplete) {
        const formInfo = this.formCompanyComponent.getFormData();
        if (!formInfo.isValid) {
          this.formCompanyComponent.formRegisterCompany.markAllAsTouched();
          return;
        }
      }
    }
    
    this.currentStep = index;
  }

  prevStep() {
    if (this.currentStep > 0) this.currentStep--;
  }
}