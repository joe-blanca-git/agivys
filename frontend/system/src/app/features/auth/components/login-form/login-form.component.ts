import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormsModule,
  ReactiveFormsModule,
} from '@angular/forms';
import { AuthService } from '../../../../core/auth/auth.service';
import { Router, RouterLinkActive, RouterModule } from '@angular/router';
import { ToastService } from '../../../../core/services/toast.service';

@Component({
  selector: 'app-login-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, RouterLinkActive, RouterModule],
  templateUrl: './login-form.component.html',
  styleUrls: ['./login-form.component.scss'],
})
export class LoginFormComponent {
  formLogin: FormGroup;
  submitted = false;
  isLoading = false;
  hidePassword = true

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private toastService: ToastService
  ) {
    this.formLogin = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', Validators.required],
    });
  }

  get f() {
    return this.formLogin.controls;
  }

  onSubmit() {
    this.submitted = true;

    this.formLogin.markAllAsTouched();

    if (this.formLogin.invalid) {
      this.toastService.error('Preencha usuário e senha pra entrar!');
      return;
    }

    this.isLoading = true;

    const email = this.formLogin.get('email')!.value || '';
    const password = this.formLogin.get('password')!.value || '';;

    this.authService.login(email, password).subscribe({
      next: (r) => this.processSuccess(r),
      error: (e) => this.processError(e),
    });
  }

  async processSuccess(response: any) {
    this.authService.LocalStorage.saveLocaleDataUser(response);
    this.router.navigate(['/home']);
    this.isLoading = false;
  }

  async processError(error: any) {
    console.error(error);
    this.isLoading = false;
  }
}
