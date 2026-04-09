import { Component, ViewChild } from '@angular/core';
import { Router, RouterModule } from '@angular/router';
import { CommonModule } from '@angular/common';
import { firstValueFrom } from 'rxjs';

import { RegisterFormComponent } from '../../components/register-form/register-form.component';
import { RegisterCompanyFormComponent } from '../../components/register-company-form/register-company-form.component';
import { AuthService } from '../../../../core/auth/auth.service';
import { AccountService } from '../../../modules/setup/pages/account/services/account.service';
import { ClientService } from '../../../modules/farms/services/client.service';
import { LocalStorageUtils } from '../../../../core/utils/localstorage';

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
  @ViewChild(RegisterCompanyFormComponent) formCompanyComponent!: RegisterCompanyFormComponent;

  private localStorage = new LocalStorageUtils();
  
  currentStep = 0;
  isLoading = false;
  
  private emailToLogin = '';
  private passwordToLogin = '';

  steps: Step[] = [
    { index: 0, name: 'Dados Pessoais', icon: 'fa-user' },
    { index: 1, name: 'Organização', icon: 'fa-building' },
  ];

  constructor(
    private authService: AuthService,
    private accountService: AccountService,
    private clientService: ClientService,
    private router: Router
  ) {}

  async nextStep() {
    if (this.currentStep !== 0) return;

    const formInfo = this.formComponent.getFormData();
    if (!formInfo.isValid) {
      this.formComponent.formRegister.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    try {
      // 1. Registro do Usuário
      const res = await firstValueFrom(this.authService.register(formInfo.userBody));
      if (res?.errors) throw res;

      this.emailToLogin = formInfo.userBody.email;
      this.passwordToLogin = formInfo.userBody.password;
      this.currentStep = 1;
    } catch (error) {
      console.error('Erro no registro de usuário:', error);
    } finally {
      this.isLoading = false;
    }
  }

  async finishStep() {
    if (this.currentStep !== 1) return;

    const formInfo = this.formCompanyComponent.getFormData();
    if (!formInfo.isValid) {
      this.formCompanyComponent.formRegisterCompany.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    try {
      // 2. Login imediato para obter Token necessário para os próximos passos
      const loginRes = await firstValueFrom(this.authService.login(this.emailToLogin, this.passwordToLogin));
      this.localStorage.saveLocaleDataUser(loginRes);

      // 3. Preparação dos corpos das requisições
      const rawData = formInfo.data;
      const companyBody = { name: rawData.organizationName };
      const addressBody = {
        description: 'Sede',
        zipCode: rawData.cep.replace(/\D/g, ''),
        street: rawData.street,
        number: rawData.number,
        city: rawData.city,
        state: rawData.state,
      };

      // 4. Cadastros que exigem Token (AccountService)
      const ressponse = await firstValueFrom(this.accountService.registerCompany(companyBody));
      await firstValueFrom(this.accountService.registerCompanyAddress(addressBody));

      // 5. Cadastro na API Agrovys (PostgreSQL)
      const user = this.localStorage.getUser();
      await firstValueFrom(this.clientService.createClientUnit({
        id: user.companyId,
        name: rawData.organizationName,
        agivysUserId: user.id
      }));

      this.router.navigate(['/home']);
    } catch (error) {
      console.error('Erro no fluxo de organização:', error);
    } finally {
      this.isLoading = false;
    }
  }

  setStep(index: number) {
    if (index < this.currentStep) this.currentStep = index;
  }

  prevStep() {
    if (this.currentStep > 0) this.currentStep--;
  }
}