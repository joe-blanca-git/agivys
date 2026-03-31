import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { LoginFormComponent } from '../../components/login-form/login-form.component';
import { Router, RouterModule } from '@angular/router';
import { ToastService } from '../../../../core/services/toast.service';
import { AuthService } from '../../../../core/auth/auth.service';
import { error } from 'console';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, LoginFormComponent, RouterModule],
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.scss', '../../auth.app.component.scss'],
})
export class LoginComponent {


}
