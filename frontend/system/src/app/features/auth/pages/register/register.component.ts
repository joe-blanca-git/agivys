import { CommonModule } from '@angular/common';
import { Component, ViewChild } from '@angular/core';
import { RouterModule } from '@angular/router';
import { RegisterFormComponent } from '../../components/register-form/register-form.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { AccountService } from '../../../modules/setup/pages/account/services/account.service';
import { lastValueFrom } from 'rxjs';
import { RegisterCompanyFormComponent } from '../../components/register-company-form/register-company-form.component';

interface Step {
  index: number;
  name: string;
  icon: string;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, RouterModule, RegisterFormComponent, RegisterCompanyFormComponent],
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.scss', '../../auth.app.component.scss'],
})
export class RegisterComponent {
  @ViewChild(RegisterFormComponent) formComponent!: RegisterFormComponent;
  
  currentStep = 0;
  isPersonalDataComplete = false;
  savedPersonalData: any = null;
  urlAccesAgiVys = '';

  steps: Step[] = [
    { index: 0, name: 'Meus Dados Pessoais', icon: 'fa-user' },
    { index: 1, name: 'Meu Negócio', icon: 'fa-building' },
    { index: 2, name: 'Meu Plano', icon: 'fa-money-bill' },
    { index: 3, name: 'Resumo', icon: 'fa-list-check' },
  ];

  constructor(
    private authService: AuthService,
    private accountService: AccountService,
  ) {}

async processRegister(formInfo: any) {
    try {
      console.log('Payload enviado:', formInfo);

      // Requisição única (POST único)
      const resRegistro: any = await lastValueFrom(
        this.authService.register(formInfo.userBody)
      );

      if (resRegistro && (resRegistro.status === 400 || resRegistro.errors)) {
        console.error('A API recusou o cadastro:', resRegistro);
        return; // Trava a tela aqui em caso de erro da API
      }

      // SUCESSO: Trava a tela em modo texto e avança a etapa
      this.savedPersonalData = formInfo.userBody;
      this.isPersonalDataComplete = true;
      this.currentStep++;

    } catch (error) {
      console.error('Falha crítica na requisição (Erro 400/500):', error);
      // A execução morre aqui em caso de erro no servidor
    }
  }

  async nextStep() {
    if (this.currentStep === 0) {
      // Se já preencheu e salvou, apenas avança
      if (this.isPersonalDataComplete) {
        this.currentStep++;
        return;
      }

      // Se não salvou ainda, valida o form e chama a API
      const formInfo = this.formComponent.getFormData();
      if (!formInfo.isValid) {
        this.formComponent.formRegister.markAllAsTouched();
        return;
      }
      
      await this.processRegister(formInfo);
      return;
    }

    if (this.currentStep < this.steps.length - 1) {
      this.currentStep++;
    }
  }

  setStep(index: number) {
    if (this.currentStep === 0 && index > 0) {
      // Impede a validação se o formulário já foi substituído pelo texto
      if (!this.isPersonalDataComplete) {
        const formInfo = this.formComponent.getFormData();
        if (!formInfo.isValid) {
          this.formComponent.formRegister.markAllAsTouched();
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