import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/auth/auth.service';
import { loggUser } from '../../models/loggUser';

export interface IOptions {
  name: string;
  description: string;
  route: string;
  icon: string;
}

@Component({
  selector: 'app-user-pannel',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './user-pannel.component.html',
  styleUrl: './user-pannel.component.scss',
})
export class UserPannelComponent {
  options: IOptions[] = [
    {
      name: 'Minha Conta',
      description:
        'Acesse as configurações de sua conta e altere e-mail, senha ou configurações de usuário!.',
      route: 'setup/account',
      icon: 'fa-solid fa-user',
    },
    {
      name: 'Configurações',
      description: 'Acesse as configurações da plataforma BIT',
      route: 'setup',
      icon: 'fa-solid fa-gear',
    },
  ];

  user: loggUser = {
    email: 'joeblanca@hotmail.com',
    name: 'JOEDER BLANCA',
  };

  constructor(private authService: AuthService){

  }

  onLogOut(){
    this.authService.logOut();
  }
}
