import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-recovery-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './recovery-password.component.html',
  styleUrl: './recovery-password.component.scss',
})
export class RecoveryPasswordComponent {
  formRecovery: FormGroup;
  submitted = false;
  isLoading = false;
  emailSent = false;
  sentToEmail = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastService: ToastService,
  ) {
    this.formRecovery = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
    });
  }

  get f() {
    return this.formRecovery.controls;
  }

  onSubmit(): void {
    this.submitted = true;
    this.formRecovery.markAllAsTouched();

    if (this.formRecovery.invalid) {
      this.toastService.error('Informe um e-mail válido para continuar!');
      return;
    }

    this.isLoading = true;
    const email = this.formRecovery.get('email')!.value;

    this.authService.forgotPassword(email).subscribe({
      next: () => this.processSuccess(email),
      error: (e) => this.processError(e),
    });
  }

  useAnotherEmail(): void {
    this.emailSent = false;
    this.submitted = false;
    this.formRecovery.reset();
  }

  private processSuccess(email: string): void {
    this.isLoading = false;
    this.sentToEmail = email;
    this.emailSent = true;
  }

  private processError(error: any): void {
    console.error(error);
    this.isLoading = false;
    this.toastService.error('Não foi possível enviar o link de recuperação. Tente novamente.');
  }
}
