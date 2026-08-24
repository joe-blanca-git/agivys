import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  FormGroup,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { NgxMaskDirective, provideNgxMask } from 'ngx-mask';
import { Router } from '@angular/router';
import { lastValueFrom } from 'rxjs';
import { AuthService } from '../../../../core/auth/auth.service';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-register-form',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, NgxMaskDirective],
  templateUrl: './register-form.component.html',
  styleUrl: './register-form.component.scss',
  providers: [provideNgxMask()],
})
export class RegisterFormComponent {
  formRegister: FormGroup;
  submitted = false;
  isLoading = false;
  hidePassword = true;
  hidePasswordConfirmation = true;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private toastService: ToastService,
    private router: Router,
  ) {
    this.formRegister = this.fb.group(
      {
        name: ['', [Validators.required]],
        email: ['', [Validators.required, Validators.email]],
        phone: ['', [Validators.required]],
        birthDate: ['', [Validators.required]],
        password: ['', [Validators.required, this.passwordStrengthValidator]],
        passwordConfirmation: ['', [Validators.required]],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  get f() {
    return this.formRegister.controls;
  }

  get passwordValue(): string {
    return this.formRegister.get('password')?.value || '';
  }

  hasMinLength(): boolean {
    return this.passwordValue.length >= 8;
  }

  hasUpperCase(): boolean {
    return /[A-Z]/.test(this.passwordValue);
  }

  hasNumber(): boolean {
    return /[0-9]/.test(this.passwordValue);
  }

  hasSpecialChar(): boolean {
    return /[^A-Za-z0-9]/.test(this.passwordValue);
  }

  async onSubmit(): Promise<void> {
    this.submitted = true;
    this.formRegister.markAllAsTouched();

    if (this.formRegister.invalid) {
      this.toastService.error('Verifique os campos destacados para continuar.');
      return;
    }

    this.isLoading = true;
    const raw = this.formRegister.getRawValue();

    const userBody = {
      name: raw.name,
      email: raw.email,
      password: raw.password,
      birthDate: new Date(raw.birthDate).toISOString(),
      phone: raw.phone,
    };

    try {
      await lastValueFrom(this.authService.register(userBody));
      this.toastService.success('Conta criada com sucesso! Faça login para continuar.');
      this.router.navigate(['/auth/login']);
    } catch (error) {
      console.error(error);
      this.toastService.error('Não foi possível concluir seu cadastro. Verifique os dados e tente novamente.');
    } finally {
      this.isLoading = false;
    }
  }

  private passwordStrengthValidator(c: AbstractControl): ValidationErrors | null {
    const v: string = c.value || '';
    const isValid =
      v.length >= 8 && /[A-Z]/.test(v) && /[0-9]/.test(v) && /[^A-Za-z0-9]/.test(v);

    return isValid ? null : { passwordStrength: true };
  }

  private passwordMatchValidator(g: AbstractControl): ValidationErrors | null {
    const password = g.get('password');
    const confirmation = g.get('passwordConfirmation');

    if (!password || !confirmation) return null;

    if (password.value !== confirmation.value) {
      confirmation.setErrors({ ...confirmation.errors, passwordMismatch: true });
      return { passwordMismatch: true };
    }

    if (confirmation.hasError('passwordMismatch')) {
      const { passwordMismatch, ...rest } = confirmation.errors || {};
      confirmation.setErrors(Object.keys(rest).length ? rest : null);
    }

    return null;
  }
}
